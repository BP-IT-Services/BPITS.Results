import { SampleApiResultStatusCode } from './sample-result-status-code';
import { SampleApiResult } from './sample-api-result';

export type MessageSeverity = 'info' | 'warn' | 'error' | 'success';
export type ToastMessageOptions = { detail?: string }
export interface IMessageService {
  /**
   *  Example interface to show how information may be communicated to the UI.
   * @param severity Severity of the message
   * @param summary Title of the message
   * @param options Optional configuration of the message
   */
  add(severity: MessageSeverity, summary: string, options?: ToastMessageOptions): void
}

export type CommonApiErrorsOptions = {
  messageService: IMessageService,
}

/**
 * Shows friendly toast messages for a given ApiResult's status code.
 * @param apiResult
 * @param options
 */
export function handleCommonApiErrors(apiResult: SampleApiResult<unknown> | SampleApiResult<void>, options: CommonApiErrorsOptions) {
  const statusCode = apiResult.statusCode;
  const messageService = options.messageService;

  switch (statusCode) {
    case SampleApiResultStatusCode.ResourceDenied:
    case SampleApiResultStatusCode.InsufficientPermissions:
      messageService.add('warn', 'Insufficient permissions', {
        detail: 'You do not have the required permissions to perform the requested action.'
      });
      break;

    case SampleApiResultStatusCode.ResourceNotFound:
      messageService.add('error', 'Resource not found', {
        detail: 'The requested resource no longer exists.'
      });
      break;

    case SampleApiResultStatusCode.ResourceExpired:
      messageService.add('error', 'Resource expired', {
        detail: 'The requested resource has expired.'
      });
      break;

    case SampleApiResultStatusCode.FunctionalityDisabled:
      messageService.add('error', 'Functionality disabled', {
        detail: 'The requested functionality has been disabled.'
      });
      break;

    case SampleApiResultStatusCode.ServerUnreachable:
      messageService.add('error', 'Server unreachable', {
        detail: 'We were unable to connect to the server. Please try again later.'
      });
      break;


    case SampleApiResultStatusCode.BadRequest:
      messageService.add('warn', 'Invalid input', {
        detail: 'Some of the information provided is invalid or incomplete. Please review to continue.'
      });
      break;

    case SampleApiResultStatusCode.GenericFailure:
    case SampleApiResultStatusCode.UnexpectedFormat:
    default:
      messageService.add('error', 'Unexpected error', {
        detail: 'We were unable to process your request. Please try again later.'
      });
      break;
  }
}
