import { HttpContext, HttpHeaders, HttpParams } from "@angular/common/http";

/**
 * Angular HttpClient options, as copied from Angular type definitions.
 */
export type HttpOptions = {
  headers?: HttpHeaders;
  context?: HttpContext;
  observe?: 'body';
  params?: HttpParams;
  reportProgress?: boolean;
  responseType?: 'json';
  withCredentials?: boolean;
  transferCache?: {
    includeHeaders?: string[];
  } | boolean;
}
