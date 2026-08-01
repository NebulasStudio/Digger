# syntax=docker/dockerfile:1.7
FROM ubuntu:24.04

ARG SERVER_BUILD_DIR=Game/Builds/LinuxServer
ARG SERVER_BINARY=SandsunderServer.x86_64

RUN apt-get update \
    && DEBIAN_FRONTEND=noninteractive apt-get install --no-install-recommends -y \
        ca-certificates \
        libgcc-s1 \
        libstdc++6 \
    && rm -rf /var/lib/apt/lists/* \
    && groupadd --gid 10001 sandsunder \
    && useradd --uid 10001 --gid sandsunder --no-create-home --shell /usr/sbin/nologin sandsunder

WORKDIR /opt/sandsunder
COPY --chown=sandsunder:sandsunder ${SERVER_BUILD_DIR}/ ./
RUN chmod 0555 "./${SERVER_BINARY}"

USER 10001:10001
EXPOSE 7777/udp

ENTRYPOINT ["./SandsunderServer.x86_64"]
CMD ["-batchmode", "-nographics", "-logFile", "-"]
