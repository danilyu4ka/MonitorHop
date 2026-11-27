# Code signing and Windows SmartScreen

MonitorHop already has an [MIT License](../LICENSE). It grants permission to use and modify the source code; it does not establish the identity of the publisher of an EXE. The Inno Setup wizard shows this license during installation.

The current GitHub release pipeline publishes unsigned files. Its SHA-256 checksum file can detect a changed download, but it does not make Windows display a verified publisher. Both `MonitorHop-Setup.exe` and the portable `MonitorHop.exe` currently have no Authenticode signature.

## What signing can achieve

A publicly trusted code-signing certificate replaces **Unknown publisher** with the validated publisher name and lets users verify that a file has not changed since signing. It does **not** immediately remove the SmartScreen *unrecognized app* warning. Microsoft says reputation builds from clean downloads and a consistent signing identity; EV certificates no longer grant an instant exception. Self-signed certificates are not useful for public downloads. [Microsoft: code-signing options](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options), [SmartScreen reputation](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation).

For Store-distributed MSIX packages, Microsoft signs the package and SmartScreen does not warn during a Store install. Submitting this project's classic EXE installer to the Store would still require our own trusted signature. [Microsoft: Store signing paths](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options).

## Practical routes for this project

1. **SignPath Foundation:** free signing for qualifying open-source projects. MonitorHop is MIT-licensed and already has public releases, so this is the first route to investigate. SignPath must approve the application. The certificate's displayed publisher would be **SignPath Foundation**, not the GitHub username. Its terms require a code-signing policy, MFA, defined project roles, verifiable builds, and manual approval of each signed release. [Eligibility and conditions](https://signpath.org/terms), [apply](https://signpath.org/apply).
2. **Azure Artifact Signing:** a paid managed service with publisher identity validation and GitHub Actions integration. Public Trust validation is currently available to organizations in the EU, including Estonia; **individual developers are limited to the US and Canada**. This is an option if MonitorHop is published by an eligible legal organization. [Microsoft: eligibility](https://learn.microsoft.com/en-us/azure/artifact-signing/quickstart).
3. **A certificate from a public CA:** suitable when the publisher can pass the CA's identity checks and use the required hardware token or HSM. Check the CA's current terms and pricing before buying. Paying extra for EV solely to avoid SmartScreen will not help. [Microsoft: traditional certificates](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options).

With only a few users, even a valid signature may leave the SmartScreen reputation warning in place for some time. We should never ask users to disable SmartScreen or change corporate endpoint protection to work around it.

## Release integration once a signer is available

Keep signing credentials outside this repository. The release workflow should sign and time-stamp the app executable in `install-payload` **before** compiling the installer; then sign the finished installer and portable EXE. Verify both published signatures with `signtool verify /pa /v`, calculate SHA-256 checksums **after** signing, and publish only verified artifacts. Windows SDK SignTool supports SHA-256 file digests and RFC 3161 time stamps; time stamps keep valid signatures verifiable after certificate expiry. [Microsoft: SignTool](https://learn.microsoft.com/en-us/windows/win32/seccrypto/using-signtool-to-sign-a-file), [time stamping](https://learn.microsoft.com/en-us/windows/win32/seccrypto/time-stamping-authenticode-signatures).

The signing workflow depends on the chosen provider. Do not add a certificate file, private key, signing password, or service credential to the public repository or a release archive.
