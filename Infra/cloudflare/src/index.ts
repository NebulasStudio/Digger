const ASSET_ROUTE = /^\/v1\/assets\/(sha256\/[a-f0-9]{64}\/[A-Za-z0-9._/-]{1,240})$/;
const ALLOWED_METHODS = "GET, HEAD, OPTIONS";

function jsonError(status: number, code: string, requestId: string): Response {
  return Response.json(
    { error: code, request_id: requestId },
    {
      status,
      headers: {
        "cache-control": "no-store",
        "content-type": "application/problem+json; charset=utf-8",
        "x-content-type-options": "nosniff",
        "x-request-id": requestId,
      },
    },
  );
}

function isSafeKey(key: string): boolean {
  return !key.split("/").some((segment) => segment === "" || segment === "." || segment === "..");
}

function assetHeaders(object: R2Object, requestId: string): Headers {
  const headers = new Headers();
  object.writeHttpMetadata(headers);
  headers.set("cache-control", "public, max-age=31536000, immutable");
  headers.set("etag", object.httpEtag);
  headers.set("x-content-type-options", "nosniff");
  headers.set("x-request-id", requestId);
  return headers;
}

export default {
  async fetch(request, env): Promise<Response> {
    const requestId = crypto.randomUUID();
    const url = new URL(request.url);

    if (url.pathname === "/health") {
      if (request.method !== "GET" && request.method !== "HEAD") {
        return new Response(null, { status: 405, headers: { allow: "GET, HEAD" } });
      }
      return new Response(request.method === "HEAD" ? null : "ok", {
        status: 200,
        headers: {
          "cache-control": "no-store",
          "content-type": "text/plain; charset=utf-8",
          "x-request-id": requestId,
        },
      });
    }

    if (request.method === "OPTIONS") {
      return new Response(null, { status: 204, headers: { allow: ALLOWED_METHODS } });
    }
    if (request.method !== "GET" && request.method !== "HEAD") {
      return new Response(null, { status: 405, headers: { allow: ALLOWED_METHODS } });
    }

    const match = ASSET_ROUTE.exec(url.pathname);
    const key = match?.[1];
    if (key === undefined || !isSafeKey(key)) {
      return jsonError(404, "asset_not_found", requestId);
    }

    if (request.method === "HEAD") {
      const object = await env.PUBLIC_ASSETS.head(key);
      if (object === null) {
        return jsonError(404, "asset_not_found", requestId);
      }
      return new Response(null, { status: 200, headers: assetHeaders(object, requestId) });
    }

    const object = await env.PUBLIC_ASSETS.get(key);
    if (object === null) {
      return jsonError(404, "asset_not_found", requestId);
    }

    return new Response(object.body, {
      status: 200,
      headers: assetHeaders(object, requestId),
    });
  },
} satisfies ExportedHandler<Env>;

