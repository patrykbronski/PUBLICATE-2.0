-- Isolated transaction: no fixture survives, no provider calls or claimed publication.
BEGIN;
DO $$
DECLARE
 owner_id uuid := '68b82a6a-d7d8-41bd-a672-b302c88d0486';
 account_id uuid := gen_random_uuid();
 target_id uuid := gen_random_uuid();
 post_id uuid := gen_random_uuid();
 variant_id uuid := gen_random_uuid();
 job_id uuid := gen_random_uuid();
 request_id uuid := gen_random_uuid();
 blocked boolean;
BEGIN
 INSERT INTO "ConnectedAccounts"("Id","OwnerId","Platform","ExternalId","Name","AccessToken","Scopes","Disconnected") VALUES(account_id,owner_id,0,'ROLLBACK-ONLY','TEST TRANSAKCYJNY','','',true);
 INSERT INTO "PublishingTargets"("Id","OwnerId","AccountId","Platform","ExternalId","Name","Kind") VALUES(target_id,owner_id,account_id,0,'ROLLBACK-ONLY','TEST TRANSAKCYJNY','Profile');
 INSERT INTO "Posts"("Id","OwnerId","Description","Cta","MediaIds","UpdatedAt") VALUES(post_id,owner_id,'TEST ROLLBACK','',ARRAY[]::uuid[],now());
 INSERT INTO "PostVariants"("Id","OwnerId","PostId","TargetId","SettingsJson") VALUES(variant_id,owner_id,post_id,target_id,'{}');
 INSERT INTO "PublicationJobs"("Id","OwnerId","VariantId","Platform","TargetName","RequestKey","SnapshotJson","MediaIds","State","SafeToRetry","CreatedAt") VALUES(job_id,owner_id,variant_id,0,'TEST TRANSAKCYJNY',request_id,'{}',ARRAY[]::uuid[],11,false,now());
 blocked := false;
 BEGIN
  INSERT INTO "PublicationJobs"("Id","OwnerId","VariantId","Platform","TargetName","RequestKey","SnapshotJson","MediaIds","State","SafeToRetry","CreatedAt") VALUES(gen_random_uuid(),owner_id,variant_id,0,'TEST',gen_random_uuid(),'{}',ARRAY[]::uuid[],5,false,now());
 EXCEPTION WHEN unique_violation THEN blocked := true;
 END;
 IF NOT blocked THEN RAISE EXCEPTION 'Active variant uniqueness failed'; END IF;
 blocked := false;
 BEGIN
  INSERT INTO "PublicationJobs"("Id","OwnerId","VariantId","Platform","TargetName","RequestKey","SnapshotJson","MediaIds","State","SafeToRetry","CreatedAt") VALUES(gen_random_uuid(),owner_id,variant_id,0,'TEST',request_id,'{}',ARRAY[]::uuid[],7,false,now());
 EXCEPTION WHEN unique_violation THEN blocked := true;
 END;
 IF NOT blocked THEN RAISE EXCEPTION 'RequestKey uniqueness failed'; END IF;
 blocked := false;
 BEGIN
  DELETE FROM "ConnectedAccounts" WHERE "Id"=account_id;
 EXCEPTION WHEN foreign_key_violation THEN blocked := true;
 END;
 IF NOT blocked THEN RAISE EXCEPTION 'Foreign key restriction failed'; END IF;
 RAISE NOTICE 'PASS PostgreSQL active variant uniqueness, request idempotency and FK restrictions';
END $$;
ROLLBACK;
