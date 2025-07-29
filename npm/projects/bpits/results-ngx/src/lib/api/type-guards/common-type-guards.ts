import { TypeGuardPredicate } from "./type-guard-predicate";

export abstract class CommonTypeGuards {
  public static basics = {
    string: (): TypeGuardPredicate<string> => (obj: unknown): obj is string => typeof obj === 'string',
    number: (): TypeGuardPredicate<number> => (obj: unknown): obj is number => typeof obj === 'number',
    boolean: (): TypeGuardPredicate<boolean> => (obj: unknown): obj is boolean => typeof obj === 'boolean',
    object: (): TypeGuardPredicate<object> => (obj: unknown): obj is object => typeof obj === 'object',

    nullableString: (): TypeGuardPredicate<string | null | undefined> => (obj: unknown): obj is string | null | undefined => obj === undefined || obj === null || typeof obj === 'string',
    nullableNumber: (): TypeGuardPredicate<number | null | undefined> => (obj: unknown): obj is number | null | undefined => obj === undefined || obj === null || typeof obj === 'number',
    nullableBoolean: (): TypeGuardPredicate<boolean | null | undefined> => (obj: unknown): obj is boolean | null | undefined => obj === undefined || obj === null || typeof obj === 'boolean',
    nullableObject: (): TypeGuardPredicate<object | null | undefined> => (obj: unknown): obj is object | null | undefined => obj === undefined || obj === null || typeof obj === 'object',
  };

  public static date = {
    date: (): TypeGuardPredicate<Date> => (obj: unknown): obj is Date => obj instanceof Date,
    dateString: (): TypeGuardPredicate<string> => (obj: unknown): obj is string => typeof obj === 'string' && new Date(obj).toString() !== 'Invalid Date',

    nullableDate: (): TypeGuardPredicate<Date | null | undefined> => (obj: unknown): obj is Date | null | undefined => obj === undefined || obj === null || obj instanceof Date,
    nullableDateString: (): TypeGuardPredicate<string | null | undefined> => (obj: unknown): obj is string | null | undefined => obj === undefined || obj === null || (typeof obj === 'string' && new Date(obj).toString() !== 'Invalid Date'),
  };

  public static array = {
    array: (): TypeGuardPredicate<Array<unknown>> => (obj: unknown): obj is Array<unknown> => Array.isArray(obj),
    arrayOf: <TChild>(typeGuard: (childObj: unknown) => childObj is TChild): TypeGuardPredicate<Array<TChild>> => (obj: unknown): obj is Array<TChild> => Array.isArray(obj) && obj.every(typeGuard),

    nullableArray: (): TypeGuardPredicate<Array<unknown> | null | undefined> => (obj: unknown): obj is Array<unknown> | null | undefined => obj === undefined || obj === null || Array.isArray(obj),
    nullableArrayOf: <TChild>(typeGuard: (childObj: unknown) => childObj is TChild): TypeGuardPredicate<Array<TChild> | null | undefined> => (obj: unknown): obj is Array<TChild> | null | undefined => obj === undefined || obj === null || Array.isArray(obj) && obj.every(typeGuard),
  };
}
