FROM golang:1.25-alpine@sha256:1ae0735f00daffa3aaf1363a5184c0d2dc55c78e3db4ec70241cdac97bf84b59 AS builder

ARG LIBRARR_UPSTREAM_COMMIT=1208254c20b31fbf217558c0fb987f779fed1cf8
RUN apk add --no-cache git
WORKDIR /build
RUN git clone --filter=blob:none https://github.com/JeremiahM37/librarr.git source \
    && cd source \
    && git checkout --detach "${LIBRARR_UPSTREAM_COMMIT}" \
    && test "$(git rev-parse HEAD)" = "${LIBRARR_UPSTREAM_COMMIT}"

COPY infrastructure/librarr/patches/0001-audiobook-import-no-overwrite.patch /tmp/no-overwrite.patch
COPY infrastructure/librarr/patches/0002-explicit-source-policy.patch /tmp/source-policy.patch
COPY infrastructure/librarr/patches/0003-durable-import-outcome.patch /tmp/import-outcome.patch
COPY infrastructure/librarr/patches/0004-author-aware-discovery.patch /tmp/author-aware-discovery.patch
COPY infrastructure/librarr/patches/0005-pinned-native-source-policy.patch /tmp/pinned-native-source-policy.patch
COPY infrastructure/librarr/patches/0006-audiobookbay-parser-boundaries.patch /tmp/audiobookbay-parser-boundaries.patch
COPY infrastructure/librarr/patches/0007-audiobookbay-query-case-normalization.patch /tmp/audiobookbay-query-case-normalization.patch
RUN cd source \
    && git apply --check /tmp/no-overwrite.patch \
    && git apply /tmp/no-overwrite.patch \
    && git apply --check /tmp/source-policy.patch \
    && git apply /tmp/source-policy.patch \
    && git apply --check /tmp/import-outcome.patch \
    && git apply /tmp/import-outcome.patch \
    && git apply --check /tmp/author-aware-discovery.patch \
    && git apply /tmp/author-aware-discovery.patch \
    && git apply --check /tmp/pinned-native-source-policy.patch \
    && git apply /tmp/pinned-native-source-policy.patch \
    && git apply --check /tmp/audiobookbay-parser-boundaries.patch \
    && git apply /tmp/audiobookbay-parser-boundaries.patch \
    && git apply --check /tmp/audiobookbay-query-case-normalization.patch \
    && git apply /tmp/audiobookbay-query-case-normalization.patch \
    && test -z "$(gofmt -l internal/organize/pipeline.go internal/organize/audiobook_conflict_test.go internal/search/source_policy.go internal/search/source_policy_test.go internal/search/audiobookbay.go internal/search/searcher.go internal/search/prowlarr.go internal/search/discovery_query_test.go internal/api/download.go internal/api/search.go internal/api/audiobook_provider_test.go internal/download/watcher.go internal/download/watcher_test.go cmd/librarr/main.go)" \
    && go test ./internal/organize ./internal/search ./internal/download \
    && go test ./internal/api -run '^TestInfoHashFromMagnetIsBoundedAndNormalized$' \
    && CGO_ENABLED=0 GOOS=linux go build -ldflags="-s -w" -o /librarr ./cmd/librarr/

FROM alpine:3.21@sha256:48b0309ca019d89d40f670aa1bc06e426dc0931948452e8491e3d65087abc07d
RUN apk add --no-cache ca-certificates tzdata \
    && adduser -D -u 1000 librarr
COPY --from=builder /librarr /usr/local/bin/librarr
COPY --chmod=0555 infrastructure/librarr/entrypoint.sh /usr/local/bin/bigbrain-librarr-entrypoint
USER librarr
EXPOSE 5050
ENTRYPOINT ["/usr/local/bin/bigbrain-librarr-entrypoint"]
