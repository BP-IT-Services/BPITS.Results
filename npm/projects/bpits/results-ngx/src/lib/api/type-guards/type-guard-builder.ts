import { TypeGuardPredicate } from "./type-guard-predicate";

export class TypeGuardBuilder<T> {
  public static LogValueReceived: boolean = true;

  private _rootValidators: Array<(obj: unknown) => obj is T> = [];
  private _validators = new Map<keyof T, Array<(obj: unknown) => obj is T[keyof T]>>();

  constructor(private readonly _rootTypeName: string) {
  }

  public validateRoot(predicate: (obj: unknown) => obj is T): this {
    this._rootValidators.push(predicate);
    return this;
  }

  public validateProperty<TProperty extends keyof T>(property: TProperty, predicate: TypeGuardPredicate<T[TProperty]>): this {
    const previousValue = this._validators.get(property) ?? [];
    this._validators.set(property, [ ...previousValue, predicate ]);
    return this;
  }

  build(): (value: unknown) => value is T {
    return (obj: unknown): obj is T => {
      if (typeof obj !== 'object')
        return false;

      if (!obj)
        return false;

      if (!this._rootValidators.every(v => v(obj))) {
        console.warn(`Validation failed for root object '${this._rootTypeName}'. Value received:`, this.sanitiseValueReceived(obj));
        return false;
      }

      const recordObj = obj as Record<keyof T, unknown>;
      const objKeys = Object.keys(obj) as Array<keyof T>;
      for (const key of objKeys) {
        const keyValidator = this._validators.get(key);
        if (!keyValidator) {
          console.warn(`No validator specified for property '${key.toString()}' in '${this._rootTypeName}'`);
          continue;
        }

        const value = recordObj[key];
        if (!keyValidator.every(v => v(value))) {
          console.warn(`Validation failed for property '${key.toString()} in '${this._rootTypeName}'. Value received:`, this.sanitiseValueReceived(value));
          return false;
        }
      }

      return true;
    };
  }

  public buildNullable(): TypeGuardPredicate<T | null | undefined> {
    return (obj: unknown): obj is T | null | undefined => {
      if (obj === null || obj === undefined)
        return true;

      return this.build()(obj);
    }
  }

  public static start<T>(typeName: string): TypeGuardBuilder<T> {
    return new TypeGuardBuilder<T>(typeName);
  }

  private sanitiseValueReceived(value: unknown): unknown {
    return TypeGuardBuilder.LogValueReceived ? value : 'redacted';
  }
}
