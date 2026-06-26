-- ============================================================================
-- Deduplicate categories by normalized_name
-- For each duplicate pair: keep the category with more transactions,
-- migrate all references to it, and delete the duplicate.
-- ============================================================================

DO $$
DECLARE
    pair RECORD;
BEGIN
    -- Process each duplicate pair
    FOR pair IN
        WITH pairs AS (
            -- KEEPER (more tx) | MERGE (fewer tx)
            SELECT 'b1024734-d971-44ef-8c2a-4a76f6228848'::uuid as keeper_id, 'c964894e-6f90-4f4f-b526-286e845a23c2'::uuid as merge_id
            UNION ALL
            SELECT '4d4d578b-d188-447f-8fb1-242c8603c227', '65cbcdf0-6d45-4962-9323-8cb6461065ea'
            UNION ALL
            SELECT '626e7624-4319-4fd3-919e-af9ee674e57e', 'cf3949c4-18ff-4392-a553-f39e67594f78'
            UNION ALL
            SELECT 'd9466e7f-8dcf-4b0c-839c-5fc43fb97c58', 'db910c53-c2be-4ed8-8e75-a4d8e37623a4'
            UNION ALL
            SELECT 'd4a5ceeb-d44c-4f37-b687-43dcd76b3952', '1a18aa4f-9ed5-4049-b2e8-4cbb065704ec'
            UNION ALL
            SELECT 'f5c1e608-f396-4656-bef6-7992c2050957', '39d83106-0b66-45df-88d0-128fd2f5dcb7'
        )
        SELECT * FROM pairs
    LOOP
        -- Step 1: For conflicting subcategories (same normalized_name in both),
        -- remap transactions pointing to merge subcategory → keeper subcategory
        UPDATE transactions t
        SET subcategory_id = sk.id
        FROM subcategories sk, subcategories sm
        WHERE sk.category_id = pair.keeper_id
          AND sm.category_id = pair.merge_id
          AND sk.normalized_name = sm.normalized_name
          AND t.subcategory_id = sm.id;

        -- Step 2: Delete conflicting subcategories from merge category
        DELETE FROM subcategories sm
        USING subcategories sk
        WHERE sk.category_id = pair.keeper_id
          AND sm.category_id = pair.merge_id
          AND sk.normalized_name = sm.normalized_name;

        -- Step 3: Move remaining (non-conflicting) subcategories to keeper category
        UPDATE subcategories
        SET category_id = pair.keeper_id
        WHERE category_id = pair.merge_id;

        -- Step 4: Remap transactions from merge category to keeper category
        UPDATE transactions
        SET category_id = pair.keeper_id
        WHERE category_id = pair.merge_id;

        -- Step 5: Remap budgets from merge category to keeper category
        UPDATE budgets
        SET category_id = pair.keeper_id
        WHERE category_id = pair.merge_id;

        -- Step 6: Delete the merge category
        DELETE FROM categories
        WHERE id = pair.merge_id;
    END LOOP;
END $$;
