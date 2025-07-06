import { HttpClient, HttpErrorResponse, HttpRequest, HttpResponse } from '@angular/common/http';
import { firstValueFrom, Observable, of, take, takeUntil } from 'rxjs';
import { ApiResult } from './models/api-result';
import { TypeGuardPredicate } from './type-guards/type-guard-predicate';
import { isApiResult } from './type-guards/is-api-result';
import { ICustomStatusCodeProvider } from './custom-status-code-provider';



export abstract class ApiClient<TResultStatusEnum> {
  protected constructor(
    protected _http: HttpClient,
    protected resultStatusCodeProvider: ICustomStatusCodeProvider<TResultStatusEnum>)
  {
  }

  /**
   * Primarily designed to work with ApiResult<T>.
   * Makes a GET HTTP request to the specified URL
   * and verifies the result is ApiResult<T>.
   * If the response fails, or receives an unexpected value,
   * an ApiResult object is created and returned.
   * @param url
   * @param valueTypeGuard Optional predicate to verify the type of the value inside ApiResult<T>.
   * @param options
   * @param cancelRequest$ Optional observable that upon emission will cancel the HTTP request
   */
  public async getAsync<TResult>(
    url: string,
    valueTypeGuard?: TypeGuardPredicate<TResult>,
    options?: HttpOptions,
    cancelRequest$?: Observable<unknown>
  ): Promise<ApiResult<TResult, TResultStatusEnum>> {
    try {
      const request = new HttpRequest<unknown>('GET', url, options);
      return await this.requestAsync(request, valueTypeGuard, cancelRequest$);
    } catch (err) {
      console.error("Request to API failed!", err);
      return this.handleRequestError(err);
    }
  }

  /**
   * Primarily designed to work with ApiResult<T>.
   * Makes a POST HTTP request to the specified URL
   * and verifies the result is ApiResult<T>.
   * If the response fails, or receives an unexpected value,
   * an ApiResult object is created and returned.
   * @param url
   * @param payload Payload/body of the POST request.
   * @param valueTypeGuard Optional predicate to verify the type of the value inside ApiResult<T>.
   * @param options
   * @param cancelRequest$ Optional observable that upon emission will cancel the HTTP request
   */
  public async postAsync<TResult>(
    url: string,
    payload: unknown,
    valueTypeGuard?: TypeGuardPredicate<TResult>,
    options?: HttpOptions,
    cancelRequest$?: Observable<unknown>
  ): Promise<ApiResult<TResult, TResultStatusEnum>> {
    try {
      const request = new HttpRequest<unknown>('POST', url, payload, options);
      return await this.requestAsync(request, valueTypeGuard, cancelRequest$);
    } catch (err) {
      console.error("Request to API failed!", err);
      return this.handleRequestError(err);
    }
  }

  /**
   * Primarily designed to work with ApiResult<T>.
   * Makes a POST HTTP request to the specified URL
   * and verifies the result is ApiResult<T>.
   * If the response fails, or receives an unexpected value,
   * an ApiResult object is created and returned.
   * @param url
   * @param payload Payload/body of the POST request as form data.
   * @param valueTypeGuard Optional predicate to verify the type of the value inside ApiResult<T>.
   * @param options
   * @param cancelRequest$ Optional observable that upon emission will cancel the HTTP request
   */
  public async postFormAsync<TResult>(
    url: string,
    payload: FormData,
    valueTypeGuard?: TypeGuardPredicate<TResult>,
    options?: HttpOptions,
    cancelRequest$?: Observable<unknown>
  ): Promise<ApiResult<TResult, TResultStatusEnum>> {
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
   * Primarily designed to work with ApiResult<T>.
   * Makes a PATCH HTTP request to the specified URL
   * and verifies the result is ApiResult<T>.
   * If the response fails, or receives an unexpected value,
   * an ApiResult object is created and returned.
   * @param url
   * @param payload Payload/body of the PATCH request.
   * @param valueTypeGuard Optional predicate to verify the type of the value inside ApiResult<T>.
   * @param options
   * @param cancelRequest$ Optional observable that upon emission will cancel the HTTP request
   */
  public async patchAsync<TResult>(
    url: string,
    payload: unknown,
    valueTypeGuard?: TypeGuardPredicate<TResult>,
    options?: HttpOptions,
    cancelRequest$?: Observable<unknown>
  ): Promise<ApiResult<TResult, TResultStatusEnum>> {
    try {
      const request = new HttpRequest<unknown>('PATCH', url, payload, options);
      return await this.requestAsync(request, valueTypeGuard, cancelRequest$);
    } catch (err) {
      console.error("Request to API failed!", err);
      return this.handleRequestError(err);
    }
  }

  /**
   * Primarily designed to work with ApiResult<T>.
   * Makes a DELETE HTTP request to the specified URL
   * and verifies the result is ApiResult<T>.
   * If the response fails, or receives an unexpected value,
   * an ApiResult object is created and returned.
   * @param url
   * @param valueTypeGuard Optional predicate to verify the type of the value inside ApiResult<T>.
   * @param options
   * @param cancelRequest$ Optional observable that upon emission will cancel the HTTP request
   */
  public async deleteAsync<TResult>(
    url: string,
    valueTypeGuard?: TypeGuardPredicate<TResult>,
    options?: HttpOptions,
    cancelRequest$?: Observable<unknown>
  ): Promise<ApiResult<TResult, TResultStatusEnum>> {
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
  ): Promise<ApiResult<TResult, TResultStatusEnum>> {
    if (!request.headers.has('Content-Type')) { // Set the content type if it hasn't already.
      request.headers.set('Content-Type', 'application/json');
    }

    let hasCancelled = false;
    const subscription = cancelRequest$?.pipe(take(1)).subscribe(() => hasCancelled = true);

    try {
      const response = await firstValueFrom(this._http.request(request).pipe(takeUntil(cancelRequest$ ?? of())));
      subscription?.unsubscribe();

      if (!(response instanceof HttpResponse) || !isApiResult<TResult, TResultStatusEnum>(response.body, valueTypeGuard)) {
        console.error('Unexpected value received from API', response);
        return this.makeApiResult({
          statusCode: this.resultStatusCodeProvider.unexpectedFormat,
          errorMessage: 'Invalid response from server'
        });
      }

      return response.body;
    } catch (err) {
      subscription?.unsubscribe();

      if (hasCancelled) {
        return this.makeApiResult({
          statusCode: this.resultStatusCodeProvider.requestCancelled,
          errorMessage: 'Request cancelled by client'
        });
      }

      throw err;
    }
  }

  /**
   * Provides a unified experience for handling request errors.
   * The provided error type will be examined and a suitable ApiResult<T> will be generated
   * in response.
   * @param err
   */
  public handleRequestError<T>(err: unknown): ApiResult<T, TResultStatusEnum> {
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
    options?: Partial<ApiResult<TValue, TResultStatusEnum>>
  ): ApiResult<TValue, TResultStatusEnum> {
    return {
      statusCode: this.resultStatusCodeProvider.genericFailure,
      value: null,
      errorMessage: null,
      errorDetails: null,
      ...options
    };
  }
}
