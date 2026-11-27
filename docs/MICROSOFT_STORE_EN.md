# Publishing MonitorHop in Microsoft Store

This branch builds an **unsigned x64 MSIX** for Partner Center. Microsoft signs MSIX packages after certification and distributes their updates. The existing GitHub/Inno Setup release is a separate channel. The Store build omits the GitHub update prompt and uses the package Startup Task for **Start with Windows** instead of the `HKCU\Run` key.

## 1. Create the Store product

1. Register at [storedeveloper.microsoft.com](https://storedeveloper.microsoft.com/), complete account verification, and open Partner Center. Choose the account type that matches how you publish: individual under your own name or company under a registered entity.
2. Open **Apps & games → New product → MSIX or PWA app**. Check and reserve the desired product name. Reserve **MonitorHop** if available. If you must choose another name, pass it as `-AppDisplayName` when building.
3. Open that product's **Product management → Product identity** page. Copy these three values exactly:

   | Partner Center value | Build parameter | GitHub Actions variable |
   | --- | --- | --- |
   | `Package/Identity/Name` | `-IdentityName` | `STORE_IDENTITY_NAME` |
   | `Package/Identity/Publisher` | `-Publisher` | `STORE_IDENTITY_PUBLISHER` |
   | `Package/Properties/PublisherDisplayName` | `-PublisherDisplayName` | `STORE_PUBLISHER_DISPLAY_NAME` |

   If the reserved app name differs from MonitorHop, also set `-AppDisplayName` or `STORE_APP_DISPLAY_NAME` to that name. **Do not guess these identity values**; a package with preview values will fail Store association.

## 2. Build the upload file

On a Windows x64 machine, install the [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) and [Windows 10/11 SDK](https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/) (which includes `MakeAppx.exe`). From the repository root, run PowerShell:

```powershell
./tools/BuildStorePackage.ps1 `
  -IdentityName 'PASTE_PACKAGE_IDENTITY_NAME' `
  -Publisher 'PASTE_FULL_PUBLISHER_VALUE' `
  -PublisherDisplayName 'PASTE_PUBLISHER_DISPLAY_NAME' `
  -AppDisplayName 'MonitorHop'
```

The script publishes the self-contained Store variant, creates the required package icons from the existing MonitorHop logo, validates the package with MakeAppx, and writes:

- `artifacts/store/MonitorHop_<version>_x64.msix` — **upload this file** on the Partner Center **Packages** page.
- `artifacts/store/Store-listing-logo-300x300.png` — **upload this image** in the Store listing's **Images** section as the logo.
- `artifacts/store/SHA256SUMS.txt` — local integrity record; do not upload it as an app package.

The output is unsigned. It is intended for **Partner Center upload only**, not direct installation or distribution. Microsoft accepts `.msix` on the Packages page, though it recommends `.msixupload` when available. No certificate or `.pfx` is needed for the Store MSIX submission. The existing `MonitorHop-Setup.exe` is **not** the package for this route.

### GitHub Actions option

The **Build Microsoft Store MSIX** workflow runs when this branch receives a push. Without identity variables, it produces a **preview package** to validate the build; preview packages cannot be submitted. After copying the three values from Product identity, enter them at **GitHub repository → Settings → Secrets and variables → Actions → Variables** using the exact variable names in the table. Set `STORE_APP_DISPLAY_NAME` if the reserved name differs. Trigger a new push to this branch, then download the `monitorhop-store-msix` artifact from the successful Actions run. Unzip it to get the `.msix` and 300×300 logo. These identity values are public package metadata, not signing secrets.

## 3. Prepare Store listing materials

In Partner Center, create a new submission and fill the required sections (the exact labels may change):

| Section | What to enter or upload |
| --- | --- |
| **Pricing and availability** | Choose markets, availability, and free pricing if you intend to keep the app free. |
| **Properties / Age ratings** | Describe the app honestly and complete the questionnaire. It is a desktop window-management utility. |
| **Packages** | Upload the newly built `.msix` with the *real* Product identity values. Target architecture is x64; the minimum OS is Windows 10 2004 (build 19041). |
| **Store listing** | Enter the reserved app name, a short description, the full description below, and category such as **Utilities & tools**. Upload the generated 300×300 logo and **at least one real PNG screenshot** of the running app at 1366×768 pixels or larger. |
| **Privacy policy / Support info** | Link to [MonitorHop privacy policy](PRIVACY_EN.md) using a public GitHub URL. Use the [issues page](https://github.com/danilyu4ka/MonitorHop/issues) for support and the [repository](https://github.com/danilyu4ka/MonitorHop) as website. |
| **Submission options** | Choose your release timing, review the summary, then submit for certification. |

Suggested short description: `Move the active window to another monitor from a small overlay or a keyboard shortcut.`

Suggested full description:

> MonitorHop places a small translucent control near the active window's title-bar buttons when two or more monitors are connected. Hover to choose another display with arrows or a map that follows your Windows monitor arrangement. The window keeps approximately the same relative position on its destination monitor. Drag the control if it covers another button. Choose a hotkey, interface language (English, Russian, or Estonian), icon opacity and size, and which visible apps to ignore. Full-screen apps are ignored by default. With one monitor, MonitorHop remains in the tray without showing its overlay.

Capture fresh screenshots from the current build on a desktop with two or more monitors. Show the overlay and, preferably, the map with an unusual layout. The small images in `docs/images` are illustrations and do **not** meet the Store's desktop screenshot size requirement. A GIF is useful for GitHub but is not a substitute for the required PNG screenshot.

## 4. Test and submit

Before submission, test the **packaged** version on a clean Windows 10/11 x64 device or through a Partner Center package flight. Check the overlay on two monitors, one-monitor hiding, different display arrangements, settings layout at 100% and 150% scaling, startup toggle, update behavior, and removal through **Settings → Apps**. A package flight is preferable for end-to-end Store installation and signing validation. Do not install the unsigned `.msix` by double-clicking it. For local development registration, build with `-KeepStaging`, enable Developer Mode, and use `Add-AppxPackage -Register <staging path>\AppxManifest.xml`; this is a developer test, not the customer installation flow.

For each update, increment the application version in `MonitorHop/MonitorHop.csproj` to a higher value, build a new package with the **same identity**, and use **Start update → Packages** in Partner Center. Store customers receive updates through Microsoft Store. Rebuild after .NET 10 security updates because the package includes its runtime.

References: [MSIX package upload](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/upload-app-packages), [product identity](https://learn.microsoft.com/en-us/windows/apps/publish/view-app-identity-details), [Store readiness checklist](https://learn.microsoft.com/en-us/windows/apps/publish/store-readiness-checklist), [desktop screenshot rules](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/screenshots-and-images), [Store signing](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options).
