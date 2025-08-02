import { BaseApiResult } from '../models/base-api-result';
import { CommonTypeGuards, StrictTypeGuardBuilder, TypeGuardBuilder, TypeGuardPredicate } from '@bpits/type-guards';

export const errorDetailsTypeGuardBuilder = StrictTypeGuardBuilder
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
  .validateProperty('errorMessage', CommonTypeGuards.basics.string.nullable())
  .validateProperty('errorDetails', errorDetailsTypeGuardBuilder.build.nullable())
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
