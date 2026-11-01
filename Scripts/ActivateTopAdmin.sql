-- ============================================================================
-- MediGuard :: Top-Admin activation helper (PostgreSQL / Supabase)
--
-- Acting admins registered through the hidden route
--   /Account/RegisterSecretAdmin
-- land with "IsActive" = false and cannot log in until a Top Admin flips the
-- flag here in the database. Run the SELECT first to review pending accounts,
-- then the UPDATE for the specific invitee.
--
-- NOTE: "IsActive" also gates Pharmacy Manager approvals (those are normally
-- approved through /Admin/PendingPharmacies). Review both before updating.
-- ============================================================================

-- 1. List every pending (inactive) account, with its Identity role assignment
SELECT u."Id", u."Email", u."UserName", u."IsActive", u."CreatedAt"::date AS "RegisteredOn",
       (SELECT string_agg(r."Name", ', ')
          FROM "AspNetUserRoles" ur
          JOIN "AspNetRoles" r ON r."Id" = ur."RoleId"
         WHERE ur."UserId" = u."Id") AS "Roles"
FROM "AspNetUsers" u
WHERE u."IsActive" = false
ORDER BY u."CreatedAt";

-- 2. Activate a specific acting admin by email (only for Admin-role invitees;
--    prefer the Approve action in the admin queue for pharmacy managers)
UPDATE "AspNetUsers"
SET "IsActive" = true
WHERE "Email" = 'acting.admin@mediguard.com';

-- 3. Verify the update landed
SELECT "Id", "Email", "UserName", "IsActive"
FROM "AspNetUsers"
WHERE "Email" = 'acting.admin@mediguard.com';
