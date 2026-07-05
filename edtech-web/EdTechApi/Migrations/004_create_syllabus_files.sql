-- Create SyllabusFiles table for storing uploaded syllabus documents
CREATE TABLE IF NOT EXISTS "SyllabusFiles" (
    "id" SERIAL PRIMARY KEY,
    "title" VARCHAR(255) NOT NULL,
    "description" TEXT,
    "file_name" VARCHAR(255) NOT NULL,
    "file_path" VARCHAR(500) NOT NULL,
    "content_type" VARCHAR(100) NOT NULL,
    "file_size" BIGINT NOT NULL,
    "uploaded_by" INTEGER REFERENCES "Users"("id") ON DELETE SET NULL,
    "created_at" TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    "updated_at" TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_syllabus_files_uploaded_by ON "SyllabusFiles"("uploaded_by");
CREATE INDEX IF NOT EXISTS idx_syllabus_files_created_at ON "SyllabusFiles"("created_at" DESC);
