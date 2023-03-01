-- Table: public.markpanels

-- DROP TABLE public.markpanels;

CREATE TABLE IF NOT EXISTS public.markpanels
(
    "id" integer NOT NULL,
	time double precision NOT NULL DEFAULT 0,
    "position" geography NOT NULL,
    "text" text COLLATE pg_catalog."default" NOT NULL,
    coalition int NOT NULL DEFAULT -1,
    groupid int NOT NULL DEFAULT -1,
    initiator text COLLATE pg_catalog."default" NOT NULL DEFAULT 'unknown',
    updated_at timestamp without time zone NOT NULL,
    CONSTRAINT markpanels_pkey PRIMARY KEY ("id")
)

TABLESPACE pg_default;

ALTER TABLE public.markpanels
    OWNER to dcscribe;