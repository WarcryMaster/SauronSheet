-- Purpose: Allow same category name with different types (Income/Expense) per user.
-- The bank can report "Transferencias" as both income (+) and expense (-).
-- Drop old unique index and recreate it including the type column.

DROP INDEX IF EXISTS uq_categories_user_normalized_name;

CREATE UNIQUE INDEX IF NOT EXISTS uq_categories_user_normalized_name_type
    ON public.categories(user_id, normalized_name, type);
