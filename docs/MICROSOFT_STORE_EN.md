# Publishing MonitorHop in Microsoft Store

This branch builds an **unsigned x64 MSIX** for Partner Center, using the reserved MonitorHop product identity. Microsoft signs MSIX packages after certification and distributes their updates. The existing GitHub/Inno Setup release is a separate channel. The Store build omits the GitHub update prompt and uses the package Startup Task for **Start with Windows** instead of the `HKCU\Run` key. Its tray menu opens the Store updates page. The classic build's tray menu opens the [MonitorHop Store listing](https://apps.microsoft.com/detail/9NX8CCC9KPG3) so users can choose to switch channels.

## 1. Create the Store product

1. Open the reserved **MonitorHop** product in [Partner Center](https://partner.microsoft.com/en-US/dashboard/products/9NX8CCC9KPG3/overview).
2. Confirm these values on **Product management → Product identity** before each submission:

   | Partner Center value | Reserved MonitorHop value |
   | --- | --- |
   | `Package/Identity/Name` | `danilyu4ka.MonitorHop` |
   | `Package/Identity/Publisher` | `CN=74354B7E-1810-49B4-A285-60F3FBB99F1A` |
   | `Package/Properties/PublisherDisplayName` | `danilyu4ka` |

   Store ID: `9NX8CCC9KPG3`. These are public package metadata, not signing secrets. Keep them unchanged for updates.

## 2. Build the upload file

On a Windows x64 machine, install the [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) and [Windows 10/11 SDK](https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/) (which includes `MakeAppx.exe`). From the repository root, run PowerShell:

```powershell
./tools/BuildStorePackage.ps1 `
  -IdentityName 'danilyu4ka.MonitorHop' `
  -Publisher 'CN=74354B7E-1810-49B4-A285-60F3FBB99F1A' `
  -PublisherDisplayName 'danilyu4ka' `
  -AppDisplayName 'MonitorHop'
```

The script publishes the self-contained Store variant, creates the required package icons from the existing MonitorHop logo, validates the package with MakeAppx, and writes:

- `artifacts/store/MonitorHop_<version>_x64.msix` — **upload this file** on the Partner Center **Packages** page.
- `artifacts/store/Store-listing-logo-300x300.png` — **upload this image** in the Store listing's **Images** section as the logo.
- `artifacts/store/SHA256SUMS.txt` — local integrity record; do not upload it as an app package.

The output is unsigned. It is intended for **Partner Center upload only**, not direct installation or distribution. Microsoft accepts `.msix` on the Packages page, though it recommends `.msixupload` when available. No certificate or `.pfx` is needed for the Store MSIX submission. The existing `MonitorHop-Setup.exe` is **not** the package for this route.

### GitHub Actions option

The **Build Microsoft Store MSIX** workflow runs on pushes to this branch and on release tags after the workflow is merged into the release branch. It already uses the real Product identity. Download the `monitorhop-store-msix` artifact from the successful Actions run and unzip it to get the `.msix` and 300×300 logo. GitHub releases alone do not update Store users: each new MSIX must also be submitted in Partner Center with a higher version and the same identity.

## 3. Prepare Store listing materials

In Partner Center, create a new submission and fill the required sections (the exact labels may change):

| Section | What to enter or upload |
| --- | --- |
| **Pricing and availability** | Choose markets, availability, and free pricing if you intend to keep the app free. |
| **Properties / Age ratings** | Describe the app honestly and complete the questionnaire. It is a desktop window-management utility. |
| **Packages** | Upload the newly built `.msix` with the *real* Product identity values. Target architecture is x64; the minimum OS is Windows 10 2004 (build 19041). |
| **Store listing** | Enter the reserved app name, a short description, the full description below, and category such as **Utilities & tools**. Upload the generated 300×300 logo and **at least one real PNG screenshot** of the running app at 1366×768 pixels or larger. |
| **Privacy policy / Support info** | Use `https://github.com/danilyu4ka/MonitorHop/blob/codex/microsoft-store/docs/PRIVACY_EN.md` for the privacy-policy URL. Use the [issues page](https://github.com/danilyu4ka/MonitorHop/issues) for support and the [repository](https://github.com/danilyu4ka/MonitorHop) as website. |
| **Submission options** | Choose your release timing, review the summary, then submit for certification. |

Suggested short description: `Move the active window to another monitor from a small overlay or a keyboard shortcut.`

Suggested full description:

> MonitorHop places a small translucent control near the active window's title-bar buttons when two or more monitors are connected. Hover to choose another display with arrows or a map that follows your Windows monitor arrangement. The window keeps approximately the same relative position on its destination monitor. Drag the control if it covers another button. Choose a hotkey, interface language (English, Russian, or Estonian), icon opacity and size, and which visible apps to ignore. Full-screen apps are ignored by default. With one monitor, MonitorHop remains in the tray without showing its overlay.

Capture fresh screenshots from the current build on a desktop with two or more monitors. Show the overlay and, preferably, the map with an unusual layout. The small images in `docs/images` are illustrations and do **not** meet the Store's desktop screenshot size requirement. A GIF is useful for GitHub but is not a substitute for the required PNG screenshot.

## 4. Test and submit

Before submission, uninstall the classic Inno Setup build on the test PC so its tray process and `HKCU\Run` entry do not conflict with the Store build. Then test the **packaged** version on a clean Windows 10/11 x64 device or through a Partner Center package flight. Check the overlay on two monitors, one-monitor hiding, different display arrangements, settings layout at 100% and 150% scaling, startup toggle, update behavior, and removal through **Settings → Apps**. A package flight is preferable for end-to-end Store installation and signing validation. Do not install the unsigned `.msix` by double-clicking it. For local development registration, build with `-KeepStaging`, enable Developer Mode, and use `Add-AppxPackage -Register <staging path>\AppxManifest.xml`; this is a developer test, not the customer installation flow. The Store build takes over the running classic copy if both are present, but the user should still remove the old installation to prevent duplicate startup entries.

For each update, increment the application version in `MonitorHop/MonitorHop.csproj` to a higher value, build a new package with the **same identity**, and use **Start update → Packages** in Partner Center. Store customers receive updates through Microsoft Store. Rebuild after .NET 10 security updates because the package includes its runtime.

References: [MSIX package upload](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/upload-app-packages), [product identity](https://learn.microsoft.com/en-us/windows/apps/publish/view-app-identity-details), [Store readiness checklist](https://learn.microsoft.com/en-us/windows/apps/publish/store-readiness-checklist), [desktop screenshot rules](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/screenshots-and-images), [Store signing](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options).
