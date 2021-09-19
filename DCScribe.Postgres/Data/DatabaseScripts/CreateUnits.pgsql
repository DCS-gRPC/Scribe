-- Table: public.units

-- DROP TABLE public.units;

CREATE TABLE IF NOT EXISTS public.units
(
    id integer NOT NULL,
    "position" geography NOT NULL,
    altitude double precision NOT NULL DEFAULT 0,
    type text COLLATE pg_catalog."default" NOT NULL,
    name text COLLATE pg_catalog."default" NOT NULL,
    callsign text COLLATE pg_catalog."default",
    player text COLLATE pg_catalog."default",
    group_name text COLLATE pg_catalog."default" NOT NULL,
    coalition integer NOT NULL,
    heading integer NOT NULL DEFAULT '-1'::integer,
    speed integer NOT NULL DEFAULT '-1'::integer,
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
    CONSTRAINT units_pkey PRIMARY KEY (id)
)

TABLESPACE pg_default;

ALTER TABLE public.units
    OWNER to dcscribe;