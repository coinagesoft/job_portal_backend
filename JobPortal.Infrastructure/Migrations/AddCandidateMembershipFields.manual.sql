-- Manual fallback for 20260812090000_AddCandidateMembershipFields.
-- Only run this directly if you are NOT applying EF migrations via
-- `dotnet ef database update` (e.g. no dotnet SDK in this environment).
-- If you run this by hand, mark the migration as applied instead of
-- re-running it through EF:
--   INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
--   VALUES ('20260812090000_AddCandidateMembershipFields', '8.0.0');

ALTER TABLE candidate_profiles
    ADD COLUMN IF NOT EXISTS is_member boolean NOT NULL DEFAULT false;

ALTER TABLE candidate_profiles
    ADD COLUMN IF NOT EXISTS membership_plan_id uuid NULL;

ALTER TABLE candidate_profiles
    ADD COLUMN IF NOT EXISTS membership_purchased_at timestamp with time zone NULL;

CREATE INDEX IF NOT EXISTS ix_candidate_profiles_membership_plan_id
    ON candidate_profiles (membership_plan_id);