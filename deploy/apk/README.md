# APK share folder

Files placed here are served by the `apk-share` Nginx container at
`http://<pi-host>:8084/`.

- Drop the mobile build here, e.g. `app-release.apk`.
- Download it on an Android device: `http://<pi-host>:8084/app-release.apk`
- Browse everything available: `http://<pi-host>:8084/`

The `.apk` files themselves are git-ignored (see repo `.gitignore`); only this
folder and its README are tracked so the mount point exists.
