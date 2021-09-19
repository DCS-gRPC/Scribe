-- Table: public.airbases

-- DROP TABLE public.airbases;

CREATE TABLE IF NOT EXISTS public.airbases
(
    "name" text COLLATE pg_catalog."default" NOT NULL,
    callsign text COLLATE pg_catalog."default",
    "position" geography NOT NULL,
    altitude double precision NOT NULL DEFAULT 0,
    "category" text COLLATE pg_catalog."default" NOT NULL,
    "type" text COLLATE pg_catalog."default" NOT NULL,
    coalition integer NOT NULL,
    updated_at timestamp without time zone NOT NULL,
    context integer NOT NULL DEFAULT 0,
    standard_identity integer NOT NULL DEFAULT 0,
    symbol_set integer NOT NULL DEFAULT 10,
    status integer NOT NULL DEFAULT 0,
    hqtf_dummy integer NOT NULL DEFAULT 0,
    amplifier integer NOT NULL DEFAULT 0,
    entity integer NOT NULL DEFAULT 0,
    entity_type integer NOT NULL DEFAULT 0,
    entity_sub_type integer NOT NULL DEFAULT 0,
    sector_one_modifier integer NOT NULL DEFAULT 0,
    sector_two_modifier integer NOT NULL DEFAULT 0,
    CONSTRAINT airbases_pkey PRIMARY KEY ("name")
)

TABLESPACE pg_default;

ALTER TABLE public.airbases
    OWNER to dcscribe;