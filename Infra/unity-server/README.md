# Unity Linux dedicated server image

The Dockerfile is intentionally a packaging boundary, not a Unity build environment. CI should produce a
Linux Server build first and then build the image from the repository root:

```sh
docker build -f Infra/unity-server/Dockerfile -t sandsunder/server:dev .
```

Expected input is `Game/Builds/LinuxServer/SandsunderServer.x86_64` plus its Unity data and shared-library
directories. Override `SERVER_BUILD_DIR` and `SERVER_BINARY` with build arguments only when the Unity build
pipeline uses different names; keep the final executable name stable or update `ENTRYPOINT` in the same PR.

The container runs as UID/GID `10001`, writes logs to stdout, and declares the default gameplay port as
UDP 7777. The server must obtain match ticket, build ID, ruleset version, map seed, Nakama endpoint and
Photon configuration from injected environment variables. Do not bake any provider credential into the
image.

Readiness must reflect the game process accepting a match, not merely a running container. Add the Edgegap
readiness integration during the networking spike; until then this image has no misleading Docker
`HEALTHCHECK`.
