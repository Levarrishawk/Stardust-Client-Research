# Prototype dependency notices

The prototype references packages rather than vendoring their source or binaries. Package archives and toolchains remain ignored. These notices do not authorize publication of original game content.

| Dependency | Pinned version | License and role |
|---|---|---|
| [BCnEncoder.Net](https://www.nuget.org/packages/BCnEncoder.Net/2.3.0) | 2.3.0 | MIT OR Unlicense; only BC7 raw block decoder API is used |
| [CommunityToolkit.HighPerformance](https://www.nuget.org/packages/CommunityToolkit.HighPerformance/8.4.0) | 8.4.0 | MIT; decoder Span2D dependency |

BCnEncoder.Net is authored by Nominom. [Upstream licensing](https://github.com/Nominom/BCnEncoder.NET/blob/74eda8a330759d81e6307f19aefc547e40687825/LICENSE-MIT) and package metadata supply the applicable terms; [CommunityToolkit license](https://github.com/CommunityToolkit/dotnet/blob/main/License.md) applies separately. Preserve applicable license/copyright notices when distributing dependencies. No distribution is performed in this task.

Package lock files record resolved versions and content hashes for Core, smoke tests and Godot. Restore from the repo root with `dotnet restore` for the selected project, `--locked-mode`, and `--packages .\local-private\nuget-packages` if using the private cache. Package retrieval may require network access; later builds/tests use `--no-restore`. The net8.0 graph selects the codec's netstandard2.1 build, without Microsoft.Bcl.Numerics. Existing Godot 4.7.2 packages retain their own upstream license terms.
