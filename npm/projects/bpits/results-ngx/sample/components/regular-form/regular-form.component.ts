import { Component } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { FormFieldComponent } from '@bpits/results-ngx';

@Component({
  selector: 'sample-regular-form',
  templateUrl: './regular-form.component.html',
  imports: [
    ReactiveFormsModule,
    FormFieldComponent
  ],
  styleUrl: './regular-form.component.less'
})
export class RegularFormComponent {
  private readonly _formBuilder = inject(FormBuilder);
  public readonly formManager = new ApiValidatedFormManager(this._formBuilder.group({
    name: new FormControl('', [Validators.required]),
    length: new FormControl('', [Validators.required, Validators.pattern('^#(?:[0-9a-fA-F]{3}){1,2}$')]),
    details: new FormControl<string | null>(null),
  }));

  public get formGroup() {
    return this.formManager.formGroup;
  }

}
