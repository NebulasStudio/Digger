declare namespace nkruntime {
  interface Context {
    env: { [key: string]: string };
    userId?: string;
    username?: string;
  }

  interface Logger {
    debug(message: string, ...args: unknown[]): void;
    info(message: string, ...args: unknown[]): void;
    warn(message: string, ...args: unknown[]): void;
    error(message: string, ...args: unknown[]): void;
  }

  interface SqlQueryResult {
    [key: string]: unknown;
  }

  interface Nakama {
    hmacSha256Hash(input: string, key: string): string;
    sqlQuery(query: string, params?: unknown[]): SqlQueryResult[];
    uuidv4(): string;
  }

  type RpcFunction = (
    ctx: Context,
    logger: Logger,
    nk: Nakama,
    payload: string
  ) => string;

  interface Initializer {
    registerRpc(id: string, fn: RpcFunction): void;
  }
}

declare var module: { exports: unknown } | undefined;
