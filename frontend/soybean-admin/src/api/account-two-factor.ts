import { requestJson } from "./request";
import type {
  AccountTwoFactorConfirmRequest,
  AccountTwoFactorDisableRequest,
  AccountTwoFactorRecoveryCodesResponse,
  AccountTwoFactorRegenerateRecoveryCodesRequest,
  AccountTwoFactorSetupResponse,
  AccountTwoFactorStatusResponse,
  ApiResult
} from "./types/generated";

export function getAccountTwoFactorStatusApi(): Promise<ApiResult<AccountTwoFactorStatusResponse>> {
  return requestJson<AccountTwoFactorStatusResponse>("/api/v1/account/2fa/status");
}

export function beginAccountTwoFactorSetupApi(): Promise<ApiResult<AccountTwoFactorSetupResponse>> {
  return requestJson<AccountTwoFactorSetupResponse>("/api/v1/account/2fa/setup", {
    method: "POST"
  });
}

export function confirmAccountTwoFactorSetupApi(
  request: AccountTwoFactorConfirmRequest
): Promise<ApiResult<AccountTwoFactorStatusResponse>> {
  return requestJson<AccountTwoFactorStatusResponse>("/api/v1/account/2fa/confirm", {
    method: "POST",
    body: JSON.stringify(request)
  });
}

export function disableAccountTwoFactorApi(
  request: AccountTwoFactorDisableRequest
): Promise<ApiResult<unknown>> {
  return requestJson<unknown>("/api/v1/account/2fa/disable", {
    method: "POST",
    body: JSON.stringify(request)
  });
}

export function regenerateAccountTwoFactorRecoveryCodesApi(
  request: AccountTwoFactorRegenerateRecoveryCodesRequest
): Promise<ApiResult<AccountTwoFactorRecoveryCodesResponse>> {
  return requestJson<AccountTwoFactorRecoveryCodesResponse>("/api/v1/account/2fa/recovery-codes/regenerate", {
    method: "POST",
    body: JSON.stringify(request)
  });
}
