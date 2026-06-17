import { usePermissionStore } from "@/stores/permission";

export function hasPermission(code: string): boolean {
  return usePermissionStore().hasPermission(code);
}

export function hasAnyPermission(codes: string[]): boolean {
  return usePermissionStore().hasAnyPermission(codes);
}

export function hasAllPermissions(codes: string[]): boolean {
  return usePermissionStore().hasAllPermissions(codes);
}
