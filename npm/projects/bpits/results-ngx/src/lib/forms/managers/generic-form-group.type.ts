import { AbstractControl } from '@angular/forms';

export type GenericFormGroup<TFormGroup> = { [K in keyof TFormGroup]: AbstractControl<unknown, unknown> };
