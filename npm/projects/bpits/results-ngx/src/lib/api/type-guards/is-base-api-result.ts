import { TypeGuardPredicate } from './type-guard-predicate';
import { BaseApiResult } from '../models/base-api-result';
import { TypeGuardBuilder } from './type-guard-builder';
import { CommonTypeGuards } from './common-type-guards';

export const errorDetailsTypeGuardBuilder = TypeGuardBuilder
  .start<Record<string, string[]>>('ErrorDetails')
  .validateRoot((obj: unknown): obj is Record<string, string[]> => {
    const recordObj = obj as Record<string, unknown>;
    for (const key of Object.keys(recordObj)) {
      if (!Array.isArray(recordObj[key]))
        return false;

      if (recordObj[key].some(e => typeof e !== 'string'))
        return false;
    }

    return true;
  });

export const baseApiResultTypeGuard = TypeGuardBuilder
  .start<BaseApiResult<unknown, unknown>>('BaseApiResult')
  .validateProperty('statusCode', CommonTypeGuards.basics.number())
  .validateProperty('errorMessage', CommonTypeGuards.basics.nullableString())
  .validateProperty('errorDetails', errorDetailsTypeGuardBuilder.buildNullable())
  .build();

export function isBaseApiResult<T, TResultStatusEnum>(obj: unknown, valueTypeGuard?: TypeGuardPredicate<T>): obj is BaseApiResult<T, TResultStatusEnum> {
  if(!baseApiResultTypeGuard(obj))
    return false;

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
