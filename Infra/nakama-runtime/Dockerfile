# syntax=docker/dockerfile:1.7
ARG NODE_IMAGE=node:22.18.0-alpine3.22
ARG ALPINE_IMAGE=alpine:3.22.1

FROM ${NODE_IMAGE} AS builder
WORKDIR /workspace

COPY Backend/package.json Backend/package-lock.json ./
RUN npm ci --ignore-scripts

COPY Backend/tsconfig.json ./
COPY Backend/src/ ./src/
RUN npm run build \
    && test -s dist/sandsunder.js

FROM ${ALPINE_IMAGE} AS module-export
RUN addgroup -S -g 10001 exporter \
    && adduser -S -D -H -u 10001 -G exporter exporter \
    && mkdir -p /artifact /output \
    && chown exporter:exporter /output

COPY --from=builder --chown=exporter:exporter /workspace/dist/sandsunder.js /artifact/sandsunder.js

USER 10001:10001
CMD ["sh", "-ec", "install -m 0444 /artifact/sandsunder.js /output/sandsunder.js"]
