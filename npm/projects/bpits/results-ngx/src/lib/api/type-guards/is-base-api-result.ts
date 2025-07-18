import { TypeGuardPredicate } from './type-guard-predicate';
import { BaseApiResult } from '../models/base-api-result';

export function isBaseApiResult<T, TResultStatusEnum>(obj: unknown, valueTypeGuard?: TypeGuardPredicate<T>): obj is BaseApiResult<T, TResultStatusEnum> {
  if (!obj || typeof obj !== 'object')
    return false;

  const recordObj = <Record<string, unknown>>obj;
  if (typeof recordObj['statusCode'] !== 'number')
    return false;


  // Check the `errorDetails` key
  if (!!recordObj['errorDetails']) {
    if (typeof recordObj['errorDetails'] !== 'object')
      return false;

    const errorDetailsRecord = recordObj['errorDetails'] as Record<string, unknown>;

    // Check the `errorDetails` values
    for (const key of Object.keys(errorDetailsRecord)) {
      if (!Array.isArray(errorDetailsRecord[key]))
        return false;

      if (errorDetailsRecord[key].some(e => typeof e !== 'string'))
        return false;
    }

    return true;
  }

  const apiResultObj = obj as BaseApiResult<unknown, TResultStatusEnum>;
  if (apiResultObj.value !== null
      && apiResultObj.value !== undefined
      && valueTypeGuard
      && !valueTypeGuard(apiResultObj.value))
  {
    return false;
  }

  return true;
}
