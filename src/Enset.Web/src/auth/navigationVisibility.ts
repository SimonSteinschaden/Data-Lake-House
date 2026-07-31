// There is no client-side claims context yet. The API remains the security boundary.
// Once JWT claims are exposed centrally, only this adapter should decide UI visibility.
export const navigationVisibility = {
  showInternalCustomerAdministration: false,
};
