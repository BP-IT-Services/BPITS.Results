import { HttpClient, HttpErrorResponse, HttpRequest, HttpResponse } from '@angular/common/http';
import { filter, firstValueFrom, Observable, take, takeUntil } from 'rxjs';
import { BaseApiResult } from './models/base-api-result';
import { TypeGuardPredicate } from './type-guards/type-guard-predicate';
import { isBaseApiResult } from './type-guards/is-base-api-result';
import { ICustomStatusCodeProvider } from './custom-status-code-provider';
import { HttpOptions } from './models/http-options';


export abstract class ApiClient<TResultStatusEnum> {
  private _baseUrl: string | null | undefined;
  public get baseUrl(): string | null | undefined {
    return this._baseUrl;
  }

  protected constructor(
    protected _http: HttpClient,
    protected resultStatusCodeProvider: ICustomStatusCodeProvider<TResultStatusEnum>,
    baseUrl?: string | null | undefined)
  {
    this.setBaseUrl(baseUrl);
  }

  public setBaseUrl(url: string | null | undefined) {
    if (!url) {
      this._baseUrl = undefined;
      return;
    }

    url = url.trim();
    this._baseUrl = url.endsWith('/')
      ? url
      : `${url}/`;
  }

  /**
   * Primarily designed to work with BaseApiResult<T>.
   * Makes a GET HTTP request to the specified URL
   * and verifies the result is BaseApiResult<T>.
   * If the response fails, or receives an unexpected value,
   * an BaseApiResult object is created and returned.
   * @param url
   * @param valueTypeGuard Optional predicate to verify the type of the value inside BaseApiResult<T>.
   * @param options
   * @param cancelRequest$ Optional observable that upon emission will cancel the HTTP request
   */
  public async getAsync<TResult>(
    url: string,
    valueTypeGuard?: TypeGuardPredicate<TResult>,
    options?: HttpOptions,
    cancelRequest$?: Observable<unknown>
  ): Promise<BaseApiResult<TResult, TResultStatusEnum>> {
    try {
      const request = new HttpRequest<unknown>('GET', url, options);
      return await this.requestAsync(request, valueTypeGuard, cancelRequest$);
    } catch (err) {
      console.error("Request to API failed!", err);
      return this.handleRequestError(err);
    }
  }

  /**
   * Primarily designed to work with BaseApiResult<T>.
   * Makes a POST HTTP request to the specified URL
   * and verifies the result is BaseApiResult<T>.
   * If the response fails, or receives an unexpected value,
   * an BaseApiResult object is created and returned.
   * @param url
   * @param payload Payload/body of the POST request.
   * @param valueTypeGuard Optional predicate to verify the type of the value inside BaseApiResult<T>.
   * @param options
   * @param cancelRequest$ Optional observable that upon emission will cancel the HTTP request
   */
  public async postAsync<TResult>(
    url: string,
    payload: unknown,
    valueTypeGuard?: TypeGuardPredicate<TResult>,
    options?: HttpOptions,
    cancelRequest$?: Observable<unknown>
  ): Promise<BaseApiResult<TResult, TResultStatusEnum>> {
    try {
      const request = new HttpRequest('POST', url, payload, options);
      return await this.requestAsync(request, valueTypeGuard, cancelRequest$);
    } catch (err) {
      console.error("Request to API failed!", err);
      return this.handleRequestError(err);
    }
  }

  /**
   * Primarily designed to work with BaseApiResult<T>.
   * Makes a POST HTTP request to the specified URL
   * and verifies the result is BaseApiResult<T>.
   * If the response fails, or receives an unexpected value,
   * an BaseApiResult object is created and returned.
   * @param url
   * @param payload Payload/body of the POST request as form data.
   * @param valueTypeGuard Optional predicate to verify the type of the value inside BaseApiResult<T>.
   * @param options
   * @param cancelRequest$ Optional observable that upon emission will cancel the HTTP request
   */
  public async postFormAsync<TResult>(
    url: string,
    payload: FormData,
    valueTypeGuard?: TypeGuardPredicate<TResult>,
    options?: HttpOptions,
    cancelRequest$?: Observable<unknown>
  ): Promise<BaseApiResult<TResult, TResultStatusEnum>> {
    try {
      const request = new HttpRequest<unknown>('POST', url, payload, options);
      request.headers.set('Content-Type', 'multipart/form-data')

      return await this.requestAsync(request, valueTypeGuard, cancelRequest$);
    } catch (err) {
      console.error("Request to API failed!", err);
      return this.handleRequestError(err);
    }
  }

  /**
   * Primarily designed to work with BaseApiResult<T>.
   * Makes a PATCH HTTP request to the specified URL
   * and verifies the result is BaseApiResult<T>.
   * If the response fails, or receives an unexpected value,
   * an BaseApiResult object is created and returned.
   * @param url
   * @param payload Payload/body of the PATCH request.
   * @param valueTypeGuard Optional predicate to verify the type of the value inside BaseApiResult<T>.
   * @param options
   * @param cancelRequest$ Optional observable that upon emission will cancel the HTTP request
   */
  public async patchAsync<TResult>(
    url: string,
    payload: unknown,
    valueTypeGuard?: TypeGuardPredicate<TResult>,
    options?: HttpOptions,
    cancelRequest$?: Observable<unknown>
  ): Promise<BaseApiResult<TResult, TResultStatusEnum>> {
    try {
      const request = new HttpRequest<unknown>('PATCH', url, payload, options);
      return await this.requestAsync(request, valueTypeGuard, cancelRequest$);
    } catch (err) {
      console.error("Request to API failed!", err);
      return this.handleRequestError(err);
    }
  }

  /**
   * Primarily designed to work with BaseApiResult<T>.
   * Makes a DELETE HTTP request to the specified URL
   * and verifies the result is BaseApiResult<T>.
   * If the response fails, or receives an unexpected value,
   * an BaseApiResult object is created and returned.
   * @param url
   * @param valueTypeGuard Optional predicate to verify the type of the value inside BaseApiResult<T>.
   * @param options
   * @param cancelRequest$ Optional observable that upon emission will cancel the HTTP request
   */
  public async deleteAsync<TResult>(
    url: string,
    valueTypeGuard?: TypeGuardPredicate<TResult>,
    options?: HttpOptions,
    cancelRequest$?: Observable<unknown>
  ): Promise<BaseApiResult<TResult, TResultStatusEnum>> {
    try {
      const request = new HttpRequest<unknown>('DELETE', url, options);
      return await this.requestAsync(request, valueTypeGuard, cancelRequest$);
    } catch (err) {
      console.error("Request to API failed!", err);
      return this.handleRequestError(err);
    }
  }

  /**
   * Executes the specified HTTP Request and validates + parses the response.
   * @param request Http Request to be sent to the server.
   * @param valueTypeGuard Type guard used for validation purposes.
   * @param cancelRequest$ Optional observable that upon emission will cancel the HTTP request
   */
  public async requestAsync<TResult>(
    request: HttpRequest<unknown>,
    valueTypeGuard?: TypeGuardPredicate<TResult>,
    cancelRequest$?: Observable<unknown>
  ): Promise<BaseApiResult<TResult, TResultStatusEnum>> {
    if (this._baseUrl) { // Prefix the URL with the Base URL if set.
      request = request.clone({ url: this._baseUrl + request.url });
    }

    if (!request.headers.has('Content-Type')) { // Set the content type if it hasn't already.
      request.headers.set('Content-Type', 'application/json');
    }

    let hasCancelled = false;
    const subscription = cancelRequest$?.pipe(take(1)).subscribe(() => hasCancelled = true);

    try {
      const httpRequest$ = cancelRequest$
        ? this._http.request(request).pipe(takeUntil(cancelRequest$))
        : this._http.request(request);

      const httpResponse$ = httpRequest$.pipe(
        filter(event => event instanceof HttpResponse),
        take(1)
      );

      const response = await firstValueFrom(httpResponse$);
      if (!(response instanceof HttpResponse) || !isBaseApiResult<TResult, TResultStatusEnum>(response.body, valueTypeGuard)) {
        console.error('Unexpected value received from API', response);
        return this.makeApiResult({
          statusCode: this.resultStatusCodeProvider.unexpectedFormat,
          errorMessage: 'Invalid response from server'
        });
      }

      return response.body;
    } catch (err) {
      if (hasCancelled) {
        return this.makeApiResult({
          statusCode: this.resultStatusCodeProvider.requestCancelled,
          errorMessage: 'Request cancelled by client'
        });
      }

      throw err;
    } finally {
      subscription?.unsubscribe();
    }
  }

  /**
   * Provides a unified experience for handling request errors.
   * The provided error type will be examined and a suitable BaseApiResult<T> will be generated
   * in response.
   * @param err
   */
  public handleRequestError<T>(err: unknown): BaseApiResult<T, TResultStatusEnum> {
    if (err instanceof HttpErrorResponse) {
      if (err.status === 0) {
        // Server could not be contacted!
        return this.makeApiResult<T>({
          statusCode: this.resultStatusCodeProvider.serverUnreachable,
          errorMessage: 'Server unreachable.'
        });
      }
    }

    return this.makeApiResult<T>({
      statusCode: this.resultStatusCodeProvider.genericFailure,
      errorMessage: `Request failed with error: ${err?.toString()}`
    });
  }

  public makeApiResult<TValue>(
    options?: Partial<BaseApiResult<TValue, TResultStatusEnum>>
  ): BaseApiResult<TValue, TResultStatusEnum> {
    return {
      statusCode: this.resultStatusCodeProvider.genericFailure,
      value: null,
      errorMessage: null,
      errorDetails: null,
      ...options
    };
  }
}
