using OopFactory.X12.FhirConverter.Api.MultiTenancy;
using OopFactory.X12.FhirConverter.Engine;

namespace OopFactory.X12.FhirConverter.Api.Services;

public class TranslationOrchestrator : ITranslationOrchestrator
{
    private readonly X12ToFhirConverter _x12ToFhir;
    private readonly FhirToX12Converter _fhirToX12;
    private readonly TenantContext _tenantContext;
    private readonly ILogger<TranslationOrchestrator> _logger;

    public TranslationOrchestrator(
        X12ToFhirConverter x12ToFhir,
        FhirToX12Converter fhirToX12,
        TenantContext tenantContext,
        ILogger<TranslationOrchestrator> logger)
    {
        _x12ToFhir = x12ToFhir;
        _fhirToX12 = fhirToX12;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public ConversionResult ConvertX12ToFhir(string x12Edi, string? transactionType = null)
    {
        _logger.LogInformation(
            "Converting X12 to FHIR. Tenant={TenantId} Type={TransactionType}",
            _tenantContext.TenantId, transactionType ?? "auto-detect");

        // Validate tenant is allowed to use this transaction type
        if (transactionType != null && !IsTransactionAllowed(transactionType))
        {
            _logger.LogWarning("Tenant {TenantId} not authorized for transaction type {Type}",
                _tenantContext.TenantId, transactionType);
            return ConversionResult.Error(
                $"Your tenant is not authorized for transaction type {transactionType}. " +
                $"Allowed types: {string.Join(", ", _tenantContext.AllowedTransactionTypes)}");
        }

        try
        {
            var result = _x12ToFhir.Convert(x12Edi, transactionType);

            if (result.IsSuccess)
                _logger.LogInformation("X12->FHIR success. Tenant={TenantId} Type={Type}",
                    _tenantContext.TenantId, result.TransactionType);
            else
                _logger.LogWarning("X12->FHIR failed. Tenant={TenantId} Error={Error}",
                    _tenantContext.TenantId, result.ErrorMessage);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "X12->FHIR error. Tenant={TenantId}", _tenantContext.TenantId);
            return ConversionResult.Error($"Conversion failed: {ex.Message}");
        }
    }

    public ConversionResult ConvertFhirToX12(string fhirJson, string transactionType, string? templateName = null)
    {
        _logger.LogInformation(
            "Converting FHIR to X12. Tenant={TenantId} Type={TransactionType}",
            _tenantContext.TenantId, transactionType);

        if (!IsTransactionAllowed(transactionType))
        {
            _logger.LogWarning("Tenant {TenantId} not authorized for transaction type {Type}",
                _tenantContext.TenantId, transactionType);
            return ConversionResult.Error(
                $"Your tenant is not authorized for transaction type {transactionType}. " +
                $"Allowed types: {string.Join(", ", _tenantContext.AllowedTransactionTypes)}");
        }

        try
        {
            var result = _fhirToX12.Convert(fhirJson, transactionType, templateName);

            if (result.IsSuccess)
                _logger.LogInformation("FHIR->X12 success. Tenant={TenantId} Type={Type}",
                    _tenantContext.TenantId, result.TransactionType);
            else
                _logger.LogWarning("FHIR->X12 failed. Tenant={TenantId} Error={Error}",
                    _tenantContext.TenantId, result.ErrorMessage);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FHIR->X12 error. Tenant={TenantId}", _tenantContext.TenantId);
            return ConversionResult.Error($"Conversion failed: {ex.Message}");
        }
    }

    private bool IsTransactionAllowed(string transactionType)
    {
        // If tenant context not set (e.g., unauthenticated paths), allow all
        if (string.IsNullOrEmpty(_tenantContext.TenantId))
            return true;

        // Allow if no restrictions configured
        if (_tenantContext.AllowedTransactionTypes == null || _tenantContext.AllowedTransactionTypes.Length == 0)
            return true;

        return _tenantContext.AllowedTransactionTypes.Contains(transactionType);
    }
}
