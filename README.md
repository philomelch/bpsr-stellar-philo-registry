# Philo's Stellar plugin registry

The `plugins.json` for philomelch's [StellarResonance](https://github.com/StellarProtocol/StellarResonanceModSystem)
plugins for *Blue Protocol: Star Resonance*, in the launcher's registry format
(`StellarResonance/docs/manifest-standard.md` § 3).

| Plugin | What it does |
|---|---|
| [Philo Lens](https://github.com/philomelch/bpsr-philo-lens) ([guide](plugins/philo-lens/guide.md)) | Check a nearby player's spec, Ability Score, season strength, Imagines and food/serum buffs from their profile card. |

## For players

In the Stellar launcher, add this URL as a plugin source:

```
https://raw.githubusercontent.com/philomelch/bpsr-stellar-philo-registry/registry/plugins.json
```

## How it works

1. Each plugin repo's `release.yml` publishes a GitHub Release (DLL, `.sha256` and `manifest.json`)
   whenever its `<Version>` changes on `main`.
2. `.github/workflows/update.yml` runs hourly at :30, on every push to `main` that changes its
   inputs, and on demand from the Actions tab. For each repo in
   `sources.json` it reads the published releases, then:
   - checks each `manifest.json`: the id is the one `sources.json` binds to that repo, and every
     URL, tag and provenance field points back at that exact repo and tag;
   - downloads each **new** version's DLL and checks its sha256 against the manifest;
   - refuses ids used by the official registry (the launcher lets a later registry override an
     earlier one by id, so a collision would shadow an official plugin);
   - merges into `plugins.json`. History is append-only, and a published version whose bytes
     change is refused. The only removals are versions withdrawn in `sources.json` (see
     "Rolling back a bad release"). A withdrawn release is skipped before
     its manifest is read, so withdrawing a release with a broken manifest also unblocks updates.
3. If anything changed, the workflow commits `plugins.json` to the **`registry` branch**, the only
   thing on that branch (the first run creates it). `main` holds the inputs (`sources.json`,
   `plugins/<id>/`, the builder), so it can require pull requests with no exception for the
   workflow. Any validation failure stops the run and leaves the published file untouched.
4. On a pull request, the same update runs as a dry run against a scratch copy, so a problem in
   `sources.json`, a guide or media file, or a listed release shows up before merge.

Releases must be public (drafts and pre-releases are ignored).

## Adding a plugin

Add it to `sources.json` and push:

```json
{ "plugins": [ { "id": "party-overlay", "repository": "<owner>/StellarPartyOverlayPlugin" } ] }
```

The `id` must match the plugin repo's `stellar-plugin.json`.

## Guide and screenshots

The launcher's plugin detail page can show a markdown guide, a screenshot/video gallery and an icon
(`manifest-standard.md` § 3). As in the official registry, they live **here**, not in the plugin
repo, in a folder per plugin:

```
plugins/party-overlay/guide.md
plugins/party-overlay/media/overview.png
```

List them in the plugin's `sources.json` entry, with paths relative to its folder, and set
`publicUrl` once (where this repo's files are served):

```json
{
  "publicUrl": "https://raw.githubusercontent.com/philomelch/bpsr-stellar-philo-registry/main/",
  "plugins": [
    { "id": "party-overlay", "repository": "<owner>/StellarPartyOverlayPlugin",
      "guide": "guide.md",
      "media": [ { "type": "image", "file": "media/overview.png", "caption": "The roster overlay." },
                 { "type": "youtube", "url": "https://www.youtube.com/watch?v=XXXXXXXXXXX" } ],
      "icon": "media/icon.png" }
  ]
}
```

- `media` entries are `image`, `video` (a `file` here or a hosted https `url`) or `youtube` (a `url`).
  `icon` is a file here or an https URL. All are optional.
- The update refuses a file that is missing, outside the plugin's folder, of an unexpected type
  (`.md` guide; `.png`/`.jpg`/`.jpeg`/`.gif`/`.webp` images; `.mp4`/`.webm` video) or too big
  (guide ≤ 1 MB, media ≤ 25 MB).
- In the guide, link screenshots relatively (`![Overview](media/overview.png)`): the launcher
  resolves them against the guide's own URL, and the same file renders on GitHub.
- Write the guide for players: what the plugin does, how to open and use it, tips.
- Editing an existing guide or image needs no update run: its URL doesn't change. Adding or
  removing one goes through `sources.json`, like any other change.
- Screenshots must not show other players' names or other real player data. Crop or blur them.

## Rolling back a bad release

Withdraw the version in `sources.json` and push to `main`:

```json
{ "id": "party-overlay", "repository": "<owner>/StellarPartyOverlayPlugin",
  "withdrawn": [ { "version": "1.1.0", "reason": "crashes on login" } ] }
```

The push starts the update workflow right away, and the version disappears from `plugins.json`. The
launcher then:

- offers the newest remaining version to new installs;
- marks any installed copy of the withdrawn version as a **required** fix at the next launch, and
  installs the newest remaining compatible version (`PreLaunchPlanner`: "its record is gone").

Players pick it up after restarting the launcher or pressing **Reload** (its registry cache is
in-memory), plus up to about 5 minutes of `raw.githubusercontent.com` caching.

Then roll forward in the plugin repo with a fixed, **higher** version. Never reuse the withdrawn
number: released versions are immutable. Leave the `withdrawn` entry in place. Deleting it puts the
version back into `plugins.json`, which is how you'd undo a withdrawal made by mistake.

If the withdrawn version was the only one, the plugin drops out of `plugins.json` entirely.
Installed copies stay where they are, and the launcher shows them as unavailable.

## Development

```bash
bash tools/check-standards.sh
dotnet build -c Release && dotnet test -c Release
# A real run into a local plugins.json (git-ignored on main; the published one is on `registry`):
git fetch origin registry && git show FETCH_HEAD:plugins.json > plugins.json
dotnet run --project src/RegistryBuilder -c Release -- sources.json plugins.json
```

## License

[AGPL-3.0-or-later](LICENSE).
