namespace Sandsunder {
  function quote(value: string): string {
    return JSON.stringify(value);
  }

  export function canonicalJson(value: unknown): string {
    if (value === null) {
      return "null";
    }
    if (typeof value === "string") {
      return quote(value);
    }
    if (typeof value === "number") {
      if (!isFinite(value)) {
        throw new Error("Canonical JSON cannot encode non-finite numbers.");
      }
      return String(value);
    }
    if (typeof value === "boolean") {
      return value ? "true" : "false";
    }
    if (Object.prototype.toString.call(value) === "[object Array]") {
      var arrayValue = value as unknown[];
      var arrayParts: string[] = [];
      for (var i = 0; i < arrayValue.length; i += 1) {
        arrayParts.push(canonicalJson(arrayValue[i]));
      }
      return "[" + arrayParts.join(",") + "]";
    }
    if (typeof value === "object") {
      var objectValue = value as { [key: string]: unknown };
      var keys = Object.keys(objectValue).sort();
      var objectParts: string[] = [];
      for (var j = 0; j < keys.length; j += 1) {
        var key = keys[j];
        if (typeof objectValue[key] !== "undefined") {
          objectParts.push(quote(key) + ":" + canonicalJson(objectValue[key]));
        }
      }
      return "{" + objectParts.join(",") + "}";
    }
    throw new Error("Unsupported value in canonical JSON.");
  }

  export function constantTimeEqual(left: string, right: string): boolean {
    var mismatch = left.length ^ right.length;
    var length = left.length > right.length ? left.length : right.length;
    for (var i = 0; i < length; i += 1) {
      mismatch |= (left.charCodeAt(i % (left.length || 1)) || 0) ^
        (right.charCodeAt(i % (right.length || 1)) || 0);
    }
    return mismatch === 0;
  }

  export function signPayload(
    payload: unknown,
    secret: string,
    hmac: (input: string, key: string) => string
  ): string {
    return "v1=" + hmac(canonicalJson(payload), secret).toLowerCase();
  }

  export function verifyEnvelope<T>(
    envelope: SignedEnvelope<T>,
    secret: string,
    hmac: (input: string, key: string) => string
  ): T {
    if (!envelope || !envelope.payload || typeof envelope.signature !== "string") {
      throw new Error("Malformed signed envelope.");
    }
    var expected = signPayload(envelope.payload, secret, hmac);
    if (!constantTimeEqual(expected, envelope.signature.toLowerCase())) {
      throw new Error("Invalid signature.");
    }
    return envelope.payload;
  }
}
