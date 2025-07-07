# @bpits/results-ngx

A robust Angular library for type-safe API result handling and form validation, designed to work seamlessly with the [BPITS.Results](https://www.nuget.org/packages/BPITS.Results) .NET library.

## Table of Contents

- [Overview](#overview)
- [Installation](#installation)
- [Quick Start](#quick-start)
  - [1. Define Your Status Code Enum](#1-define-your-status-code-enum)
  - [2. Create Your API Client](#2-create-your-api-client)
  - [3. Build Your Services](#3-build-your-services)
  - [4. Create Forms with Validation](#4-create-forms-with-validation)
- [Core Concepts](#core-concepts)
  - [BaseApiResult](#baseapiresult)
  - [ApiClient](#apiclient)
  - [Form Managers](#form-managers)
- [API Client Usage](#api-client-usage)
  - [Making HTTP Requests](#making-http-requests)
  - [Type Guards and Validation](#type-guards-and-validation)
  - [Request Cancellation](#request-cancellation)
  - [Error Handling](#error-handling)
- [Form Management](#form-management)
  - [Basic Form Validation](#basic-form-validation)
  - [API Validation Integration](#api-validation-integration)
  - [Multi-Step Forms](#multi-step-forms)
  - [Form Field Component](#form-field-component)
- [Advanced Patterns](#advanced-patterns)
  - [Custom Error Handling](#custom-error-handling)
  - [Form Change Tracking](#form-change-tracking)
  - [Validation Message Customization](#validation-message-customization)
- [Best Practices](#best-practices)
- [Integration with Backend](#integration-with-backend)

## Overview

`@bpits/results-ngx` provides a comprehensive solution for handling API responses and form validation in Angular applications.
It offers:

- **Type-safe API responses** with `BaseApiResult<T, TStatusCode>`
- **Integrated form validation** that combines Angular Reactive Forms with server-side validation
- **Automatic error handling** and display
- **Multi-step form support** with validation state management
- **Request cancellation** capabilities
- **Consistent error patterns** across your application

## Installation

```bash
npm install @bpits/results-ngx
```

## Quick Start

### 1. Define Your Status Code Enum

Create an enum that matches your backend status codes. **Important**: Your enum must include specific values that the library
requires to automatically handle network errors and unexpected responses:

```typescript
// api/app-status-code.ts
export enum AppStatusCode {
  Ok = 1,
  GenericFailure = 2,
  BadRequest = 3,
  Unauthorized = 4,
  NotFound = 5,

  // These client-only status codes are REQUIRED by the library
  // for automatic error handling:
  RequestCancelled = 65533,  // When requests are cancelled
  UnexpectedFormat = 65534,  // When responses don't match expected format
  ServerUnreachable = 65535, // When network requests fail
}
```

The library automatically handles network failures, request cancellations, and malformed responses behind the scenes.
It needs to know which status codes to use for these scenarios, which is where the `ICustomStatusCodeProvider` (next step)
comes in to create the mapping.

### 2. Create Your API Types and Client

Create your app-specific types and API client:

```typescript
// api/status-code-provider.ts
import { ICustomStatusCodeProvider } from '@bpits/results-ngx';
import { AppStatusCode } from './app-status-code';

export class AppStatusCodeProvider implements ICustomStatusCodeProvider<AppStatusCode> {
  readonly serverUnreachable = AppStatusCode.ServerUnreachable;
  readonly unexpectedFormat = AppStatusCode.UnexpectedFormat;
  readonly requestCancelled = AppStatusCode.RequestCancelled;
  readonly badRequest = AppStatusCode.BadRequest;
  readonly authenticationTokenInvalid = AppStatusCode.Unauthorized;
  readonly genericFailure = AppStatusCode.GenericFailure;
}
```

The `ICustomStatusCodeProvider` creates a bridge between your enum values and the library's automatic error handling.
When network requests fail or responses are malformed, the library uses these mappings to set the appropriate status code.

```typescript
// api/app-api-result.ts
import { BaseApiResult } from '@bpits/results-ngx';
import { AppStatusCode } from './app-status-code';

export type AppApiResult<T> = BaseApiResult<T, AppStatusCode>;
```

This type alias eliminates boilerplate throughout your application. Instead of writing `BaseApiResult<User, AppStatusCode>` everywhere,
you can write `AppApiResult<User>`. The types remain fully interchangeable while dramatically improving readability.

```typescript
// api/app-api-client.ts
import { HttpClient } from '@angular/common/http';
import { ApiClient } from '@bpits/results-ngx';
import { AppStatusCode } from './app-status-code';
import { AppStatusCodeProvider } from './status-code-provider';

export class AppApiClient extends ApiClient<AppStatusCode> {
  constructor(http: HttpClient) {
    super(http, new AppStatusCodeProvider(), 'https://api.your-domain.com/');
  }
}
```

Extending `ApiClient` bakes in your status code enum and provider, eliminating the need to specify these generics repeatedly.
This creates a clean, app-specific API for making HTTP requests.

### 3. Build Your Services

Create services that use your API client. For best practice, define type guards for all your models - the library automatically validates responses using these guards:

```typescript
// services/user.service.ts
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AppApiClient } from '../api/app-api-client';
import { AppApiResult } from '../api/app-api-result';
import { AppStatusCode } from '../api/app-status-code';

export interface User {
  id: string;
  name: string;
  email: string;
}

export interface CreateUserRequest {
  name: string;
  email: string;
}

// Type guard for User - crucial for automatic response validation
function isUser(obj: unknown): obj is User {
  if (!obj || typeof obj !== 'object') return false;
  const user = obj as Record<string, unknown>;
  return typeof user.id === 'string' && 
         typeof user.name === 'string' && 
         typeof user.email === 'string';
}

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly _api = inject(AppApiClient);

  async getUserAsync(
    userId: string,
    cancelRequest$?: Observable<unknown>
  ): Promise<AppApiResult<User>> {
    try {
      const url = `users/${encodeURIComponent(userId)}`;
      return await this._api.getAsync(url, isUser, undefined, cancelRequest$);
    } catch (err) {
      console.error("Failed to get user!", err);
      return this._api.handleRequestError(err);
    }
  }

  async createUserAsync(
    request: CreateUserRequest,
    cancelRequest$?: Observable<unknown>
  ): Promise<AppApiResult<User>> {
    try {
      return await this._api.postAsync('users', request, isUser, undefined, cancelRequest$);
    } catch (err) {
      console.error("Failed to create user!", err);
      return this._api.handleRequestError(err);
    }
  }
}
```

The library automatically calls your type guard on every successful response. If the server returns data that doesn't match
your expected structure, the library automatically returns an `AppApiResult` with `statusCode: AppStatusCode.UnexpectedFormat`.
This protects your application from malformed API responses and provides additional type safety beyond TypeScript's compile-time
checks.

### 4. Create Your Form Managers

Create app-specific form managers that eliminate boilerplate and automatically connect Angular Reactive Forms with API validation:

```typescript
// forms/app-form-managers.ts
import { 
  BaseApiValidatedFormManager, 
  BaseApiValidatedStepFormManager,
  GenericFormGroup 
} from '@bpits/results-ngx';
import { AppStatusCode } from '../api/app-status-code';

export class AppApiValidatedFormManager<TFormGroup extends GenericFormGroup<TFormGroup>>
  extends BaseApiValidatedFormManager<TFormGroup, AppStatusCode> {
  // Inherit base behaviour and extend/override if necessary
  // You can add app-specific form validation logic here
}

export class AppApiValidatedStepFormManager<TFormGroup extends GenericFormGroup<TFormGroup>> 
  extends BaseApiValidatedStepFormManager<TFormGroup, AppStatusCode> {
  // Inherit base behaviour and extend/override if necessary
  // You can add app-specific step form logic here
}
```

These form managers provide several key benefits: they reduce boilerplate by eliminating repetitive validation checking code,
automatically integrate with API responses so server validation errors appear on the correct form fields when you call
`applyApiValidationErrors(apiResult)`, seamlessly blend client-side (Angular Validators) and server-side validation errors,
and bake in your status code enum for better type safety.

### 5. Create Forms with Validation

Use the form managers and `FormField` component for integrated validation. The `FormField` component automatically displays
validation errors from both Angular Reactive Forms and API responses:

```typescript
// components/user-form.component.ts
import { Component, inject } from '@angular/core';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormFieldComponent } from '@bpits/results-ngx';
import { AppApiValidatedFormManager } from '../forms/app-form-managers';
import { UserService, CreateUserRequest } from '../services/user.service';
import { AppStatusCode } from '../api/app-status-code';

@Component({
  selector: 'app-user-form',
  standalone: true,
  imports: [ ReactiveFormsModule, FormFieldComponent ],
  template: `
    <form [formGroup]="formGroup" (ngSubmit)="submitAsync()">
      <app-form-field 
        field="name"
        [formManager]="formManager"
        [formErrorMessages]="{required: 'Name is required'}">
        <label for="name">Name *</label>
        <input 
          id="name" 
          formControlName="name"
          [class]="formManager.getInvalidFieldClasses('name')" />
      </app-form-field>

      <app-form-field 
        field="email"
        [formManager]="formManager"
        [formErrorMessages]="{required: 'Email is required', email: 'Invalid email format'}">
        <label for="email">Email *</label>
        <input 
          id="email" 
          type="email"
          formControlName="email"
          [class]="formManager.getInvalidFieldClasses('email')" />
      </app-form-field>

      <button type="submit" [disabled]="formManager.isSubmitted && formGroup.invalid">
        Create User
      </button>
    </form>
  `
})
export class UserFormComponent {
  private readonly _userService = inject(UserService);
  private readonly _formBuilder = inject(FormBuilder);

  public readonly formManager = new AppApiValidatedFormManager(
    this._formBuilder.group({
      name: new FormControl<string>('', [ Validators.required ]),
      email: new FormControl<string>('', [ Validators.required, Validators.email ])
    })
  );

  public get formGroup() {
    return this.formManager.formGroup;
  }

  public async submitAsync() {
    if (!this.formManager.validate()) {
      return; // Client-side validation failed
    }

    const formValue = this.formGroup.value;
    const request: CreateUserRequest = {
      name: formValue.name!,
      email: formValue.email!
    };

    const result = await this._userService.createUserAsync(request);

    if (result.statusCode === AppStatusCode.Ok) {
      // Handle success
      console.log('User created:', result.value);
      this.formManager.reset();
    } else if (result.statusCode === AppStatusCode.BadRequest) {
      // This is the magic: server validation errors automatically 
      // appear on the correct form fields!
      this.formManager.applyApiValidationErrors(result);
    } else {
      // Handle other errors
      console.error('Failed to create user:', result.errorMessage);
    }
  }
}
```

**How the `FormField` component works**: The component requires a form manager parameter and automatically shows validation
errors from three sources: Angular Validators (required, email, etc.) with your custom error messages, API validation errors
from `result.errorDetails` when you call `applyApiValidationErrors()`, and global form-level validation errors.
Field-specific errors take precedence over global errors, and you just need to wrap your form inputs with `<app-form-field>`
- errors appear automatically with zero additional configuration.

The key insight here is that when your server returns validation errors in the `errorDetails` object
(like `{"email": ["Email address is already in use"]}`), calling `formManager.applyApiValidationErrors(result)` makes those
errors automatically appear below the correct form fields. No manual error handling required!


## Core Concepts

### BaseApiResult

The core type for all API responses. You should create your own type alias:

```typescript
// Your app-specific type
export type AppApiResult<T> = BaseApiResult<T, AppStatusCode>;

// Base type structure
type BaseApiResult<T, TResultStatusEnum> = {
  statusCode: TResultStatusEnum;
  value: T | null;
  errorMessage: string | null;
  errorDetails: Record<string, string[]> | null;
}
```

**Properties:**

- `statusCode`: Enum value indicating the result status
- `value`: The actual data (null for failures)
- `errorMessage`: General error message
- `errorDetails`: Field-specific validation errors

### ApiClient

Base class for making HTTP requests with automatic result parsing. You should extend this with your own implementation:

```typescript
// Your app-specific API client
export class AppApiClient extends ApiClient<AppStatusCode> {
  constructor(http: HttpClient) {
    super(http, new AppStatusCodeProvider(), 'https://api.your-domain.com/');
  }
}

// Base class methods available:
abstract class ApiClient<TResultStatusEnum> {
  // HTTP methods
  getAsync<T>(url, typeGuard?, options?, cancelRequest$?): Promise<BaseApiResult<T, TResultStatusEnum>>

  postAsync<T>(url, payload, typeGuard?, options?, cancelRequest$?): Promise<BaseApiResult<T, TResultStatusEnum>>

  patchAsync<T>(url, payload, typeGuard?, options?, cancelRequest$?): Promise<BaseApiResult<T, TResultStatusEnum>>

  deleteAsync<T>(url, typeGuard?, options?, cancelRequest$?): Promise<BaseApiResult<T, TResultStatusEnum>>

  // Error handling
  handleRequestError<T>(error): BaseApiResult<T, TResultStatusEnum>
}
```

### Form Managers

You should create app-specific form managers that extend the base classes:

```typescript
// Your app-specific form managers
export class AppApiValidatedFormManager<TFormGroup extends GenericFormGroup<TFormGroup>>
  extends BaseApiValidatedFormManager<TFormGroup, AppStatusCode> {
  // Add your app-specific validation logic here
}

export class AppApiValidatedStepFormManager<TFormGroup extends GenericFormGroup<TFormGroup>>
  extends BaseApiValidatedStepFormManager<TFormGroup, AppStatusCode> {
  // Add your app-specific step form logic here
}
```

**BaseApiValidatedFormManager**: Integrates Angular Reactive Forms with API validation  
**BaseApiValidatedStepFormManager**: Extends the base manager for multi-step forms

## API Client Usage

### Making HTTP Requests

```typescript
// GET request with type validation
const userResult = await apiClient.getAsync('users/123', isUser);

// POST request with payload
const createResult = await apiClient.postAsync('users', createRequest, isUser);

// PATCH request
const updateResult = await apiClient.patchAsync('users/123', updateData, isUser);

// DELETE request
const deleteResult = await apiClient.deleteAsync('users/123');
```

### Type Guards and Validation

Type guards ensure response data matches expected types:

```typescript
function isUser(obj: unknown): obj is User {
  if (!obj || typeof obj !== 'object') return false;
  const user = obj as Record<string, unknown>;
  return typeof user.id === 'string' &&
    typeof user.name === 'string' &&
    typeof user.email === 'string';
}

// Use with API calls
const result = await apiClient.getAsync('users/123', isUser);
if (result.statusCode === AppStatusCode.Ok) {
  // result.value is guaranteed to be User type
  console.log(result.value.name);
}
```

### Request Cancellation

Cancel requests using observables:

```typescript
import { Subject, takeUntil } from 'rxjs';

const cancelSubject = new Subject<void>();

// Start request
const resultPromise = apiClient.getAsync('users', isUserArray, undefined, cancelSubject);

// Cancel after 5 seconds
setTimeout(() => cancelSubject.next(), 5000);

const result = await resultPromise;
if (result.statusCode === AppStatusCode.RequestCancelled) {
  console.log('Request was cancelled');
}
```

### Error Handling

```typescript
async function handleApiCall() {
  const result = await userService.getUserAsync('123');

  switch (result.statusCode) {
    case AppStatusCode.Ok:
      // Handle success
      return result.value;

    case AppStatusCode.NotFound:
      // Handle not found
      showMessage('User not found');
      break;

    case AppStatusCode.ServerUnreachable:
      // Handle network issues
      showMessage('Server unreachable. Please try again.');
      break;

    default:
      // Handle unexpected errors
      showMessage('An unexpected error occurred');
      break;
  }
}
```

## Form Management

### Basic Form Validation

```typescript
export class MyFormComponent {
  public readonly formManager = new AppApiValidatedFormManager(
    this.formBuilder.group({
      name: [ '', Validators.required ],
      email: [ '', [ Validators.required, Validators.email ] ]
    })
  );

  async submit() {
    if (!this.formManager.validate()) {
      return; // Form has client-side validation errors
    }

    // Form is valid, proceed with submission
    const result = await this.submitToApi(this.formManager.formGroup.value);

    if (result.statusCode === AppStatusCode.BadRequest) {
      // Apply server validation errors
      this.formManager.applyApiValidationErrors(result);
    }
  }
}
```

### API Validation Integration

Server validation errors are automatically applied to form fields:

```typescript
// Server returns validation errors
const result = {
  statusCode: AppStatusCode.BadRequest,
  value: null,
  errorMessage: "Validation failed",
  errorDetails: {
    "email": [ "Email address is already in use" ],
    "name": [ "Name must be at least 2 characters" ]
  }
};

// Apply to form
this.formManager.applyApiValidationErrors(result);

// Errors are now displayed in FormFieldComponents
```

### Multi-Step Forms

```typescript
import { ControlStepMap } from '@bpits/results-ngx';
import { AppApiValidatedStepFormManager } from '../forms/app-form-managers';

const stepMap = new ControlStepMap({
  name: 1,
  email: 1,
  address: 2,
  phone: 2,
  preferences: 3
});

const stepFormManager = new AppApiValidatedStepFormManager(formGroup, stepMap);

// Validate current step before moving to next
const canMoveToNext = stepFormManager.validateCanMoveToNextStep(currentStep);

// Find earliest step with validation errors
const errorStep = stepFormManager.findEarliestStepWithValidationError(apiResult);
```

### Form Field Component

The `FormFieldComponent` automatically displays validation errors:

```html

<app-form-field
  field="email"
  [formManager]="formManager"
  [formErrorMessages]="{
    required: 'Email is required',
    email: 'Please enter a valid email'
  }">
  <label for="email">Email Address</label>
  <input
    id="email"
    type="email"
    formControlName="email"
    [class]="formManager.getInvalidFieldClasses('email')" />
</app-form-field>
```

## Advanced Patterns

### Custom Error Handling

Create reusable error handling utilities:

```typescript
// utils/error-handler.ts
import { AppApiResult } from '../api/app-api-result';

export function handleCommonApiErrors(
  result: AppApiResult<unknown>,
  messageService: MessageService
)
{
  switch (result.statusCode) {
    case AppStatusCode.Unauthorized:
      messageService.showError('You are not authorized to perform this action');
      break;
    case AppStatusCode.NotFound:
      messageService.showError('The requested resource was not found');
      break;
    case AppStatusCode.ServerUnreachable:
      messageService.showError('Unable to connect to server. Please try again.');
      break;
    default:
      messageService.showError('An unexpected error occurred');
      break;
  }
}

// Use in components
const result = await this.userService.createUserAsync(request);
if (result.statusCode !== AppStatusCode.Ok) {
  handleCommonApiErrors(result, this.messageService);
}
```

### Form Change Tracking

Track and restore form changes:

```typescript
export class FormComponent implements OnInit, OnDestroy {
  ngOnInit() {
    // Enable change tracking
    this.formManager.trackFormChanges(true);

    // Subscribe to changes
    this.formManager.onFormValueChanged$.subscribe(event => {
      if (event.hasChanged) {
        console.log('Form has unsaved changes');
      }
    });
  }

  ngOnDestroy() {
    this.formManager.onDestroy();
  }

  restoreChanges() {
    this.formManager.restoreTrackedChanges();
  }

  resetChanges() {
    this.formManager.resetTrackedChanges();
  }
}
```

### Validation Message Customization

Use the pipe directly for custom validation display:

```typescript
// In component template
{
  {
    apiResult | apiValidationMessage
  :
    'fieldName'
  }
}

// In component code
import { ApiValidationMessagePipe } from '@bpits/results-ngx';

const hasError = ApiValidationMessagePipe.hasError(apiResult, 'email');
const errors = ApiValidationMessagePipe.getErrors(apiResult, 'email');
```

## Best Practices

### 1. Consistent Status Code Definitions

Keep your frontend status codes synchronized with your backend:

```typescript
// Ensure these match your backend enum exactly
export enum AppStatusCode {
  Ok = 1,
  GenericFailure = 2,
  BadRequest = 3,
  // ... other codes
}
```

### 2. Type Guards for All API Responses

Always use type guards to ensure type safety:

```typescript
function isUserArray(obj: unknown): obj is User[] {
  return Array.isArray(obj) && obj.every(isUser);
}
```

### 3. Centralized Error Handling

Create utility functions for common error scenarios:

```typescript
export const ApiErrorHandlers = {
  handleStandardErrors: (result: AppApiResult<unknown>) => {
    // Handle common patterns
  },

  handleFormErrors: (result: AppApiResult<unknown>, formManager: AppApiValidatedFormManager<any>) => {
    if (result.statusCode === AppStatusCode.BadRequest) {
      formManager.applyApiValidationErrors(result);
      return true;
    }
    return false;
  }
};
```

### 4. Proper Cleanup

Always clean up form managers and subscriptions:

```typescript
export class MyComponent implements OnDestroy {
  ngOnDestroy() {
    this.formManager.onDestroy();
  }
}
```

### 5. Request Cancellation for Navigation

Cancel pending requests when users navigate away:

```typescript
export class MyComponent implements OnDestroy {
  private readonly destroy$ = new Subject<void>();

  async loadData() {
    const result = await this.apiClient.getAsync('data', typeGuard, undefined, this.destroy$);
    // Handle result
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
```

## Integration with Backend

This library is designed to work seamlessly with the [BPITS.Results](https://www.nuget.org/packages/BPITS.Results) .NET library. When your backend returns `ApiResult<T>` responses, they will be automatically parsed and validated by the frontend.

**Backend (.NET):**

```csharp
[HttpPost]
public async Task<ApiResult<UserDto>> CreateUser([FromBody] CreateUserRequest request)
{
    var result = await _userService.CreateUserAsync(request);
    return ApiResult.FromServiceResult(result.MapValue(u => u?.ToDto()));
}
```

**Frontend (Angular):**

```typescript
const result = await this.userService.createUserAsync(request);
// result is automatically typed as AppApiResult<User>
```

The status codes, error messages, and validation details flow seamlessly from backend to frontend, providing a consistent developer experience across your entire application stack.
