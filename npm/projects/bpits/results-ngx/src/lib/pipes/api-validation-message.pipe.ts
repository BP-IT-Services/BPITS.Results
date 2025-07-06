import { Pipe, PipeTransform } from '@angular/core';
import { ApiResult } from "../api/models/api-result";

@Pipe({
  name: 'apiValidationMessage',
  standalone: true
})
export class ApiValidationMessagePipe implements PipeTransform {
  public transform(value: Readonly<ApiResult<unknown, unknown> | null | undefined>, field: string): string {
    if (!value)
      return '';

    const fieldErrors = ApiValidationMessagePipe.getErrors(value, field);
    if (fieldErrors.length < 1)
      return '';

    return fieldErrors.join(', ');
  }

  public static hasError(value: ApiResult<unknown, unknown>, field: string): boolean {
    return this.getErrors(value, field).length > 0;
  }

  public static getErrors(value: ApiResult<unknown, unknown>, field: string): string[] {
    if (!value.errorDetails)
      return [];

    const desiredField = field.toLowerCase();
    for (const fieldName of Object.keys(value.errorDetails)) {
      if (desiredField != fieldName.toLowerCase())
        continue;

      return value.errorDetails[fieldName];
    }

    return [];
  }
}
