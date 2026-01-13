# ServiceResult API Reference

Complete API documentation for ServiceResult<T>.

## Factory Methods
- Success(value), Success()
- Failure(error, status), Failure(ex, error)
- ValidationFailure(...)

## Properties
- IsSuccess, IsFailure
- Value, StatusCode, ErrorMessage
- ErrorDetails, Exception

## Methods  
- TryGet(out value)
- MapValue<TNew>(...)
- PassThroughFail<TNew>()
