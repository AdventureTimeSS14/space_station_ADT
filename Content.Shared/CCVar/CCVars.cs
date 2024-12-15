using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

        /// <summary>
        ///     Whether explosive depressurization will cause the grid to gain an impulse.
        ///     Needs <see cref="MonstermosEqualization"/> and <see cref="MonstermosDepressurization"/> to be enabled to work.
        /// </summary>
        public static readonly CVarDef<bool> AtmosGridImpulse =
            CVarDef.Create("atmos.grid_impulse", false, CVar.SERVERONLY);

        /// <summary>
        ///     What fraction of air from a spaced tile escapes every tick.
        ///     1.0 for instant spacing, 0.2 means 20% of remaining air lost each time
        /// </summary>
        public static readonly CVarDef<float> AtmosSpacingEscapeRatio =
            CVarDef.Create("atmos.mmos_spacing_speed", 0.15f, CVar.SERVERONLY);

        /// <summary>
        ///     Minimum amount of air allowed on a spaced tile before it is reset to 0 immediately in kPa
        ///     Since the decay due to SpacingEscapeRatio follows a curve, it would never reach 0.0 exactly
        ///     unless we truncate it somewhere.
        /// </summary>
        public static readonly CVarDef<float> AtmosSpacingMinGas =
            CVarDef.Create("atmos.mmos_min_gas", 2.0f, CVar.SERVERONLY);

        /// <summary>
        ///     How much wind can go through a single tile before that tile doesn't depressurize itself
        ///     (I.e spacing is limited in large rooms heading into smaller spaces)
        /// </summary>
        public static readonly CVarDef<float> AtmosSpacingMaxWind =
            CVarDef.Create("atmos.mmos_max_wind", 500f, CVar.SERVERONLY);

        /// <summary>
        ///     Whether atmos superconduction is enabled.
        /// </summary>
        /// <remarks> Disabled by default, superconduction is awful. </remarks>
        public static readonly CVarDef<bool> Superconduction =
            CVarDef.Create("atmos.superconduction", false, CVar.SERVERONLY);

        /// <summary>
        ///     Heat loss per tile due to radiation at 20 degC, in W.
        /// </summary>
        public static readonly CVarDef<float> SuperconductionTileLoss =
            CVarDef.Create("atmos.superconduction_tile_loss", 30f, CVar.SERVERONLY);

        /// <summary>
        ///     Whether excited groups will be processed and created.
        /// </summary>
        public static readonly CVarDef<bool> ExcitedGroups =
            CVarDef.Create("atmos.excited_groups", true, CVar.SERVERONLY);

        /// <summary>
        ///     Whether all tiles in an excited group will clear themselves once being exposed to space.
        ///     Similar to <see cref="MonstermosDepressurization"/>, without none of the tile ripping or
        ///     things being thrown around very violently.
        ///     Needs <see cref="ExcitedGroups"/> to be enabled to work.
        /// </summary>
        public static readonly CVarDef<bool> ExcitedGroupsSpaceIsAllConsuming =
            CVarDef.Create("atmos.excited_groups_space_is_all_consuming", false, CVar.SERVERONLY);

        /// <summary>
        ///     Maximum time in milliseconds that atmos can take processing.
        /// </summary>
        public static readonly CVarDef<float> AtmosMaxProcessTime =
            CVarDef.Create("atmos.max_process_time", 3f, CVar.SERVERONLY);

        /// <summary>
        ///     Atmos tickrate in TPS. Atmos processing will happen every 1/TPS seconds.
        /// </summary>
        public static readonly CVarDef<float> AtmosTickRate =
            CVarDef.Create("atmos.tickrate", 15f, CVar.SERVERONLY);

        /// <summary>
        ///     Scale factor for how fast things happen in our atmosphere
        ///     simulation compared to real life. 1x means pumps run at 1x
        ///     speed. Players typically expect things to happen faster
        ///     in-game.
        /// </summary>
        public static readonly CVarDef<float> AtmosSpeedup =
            CVarDef.Create("atmos.speedup", 8f, CVar.SERVERONLY);

        /// <summary>
        ///     Like atmos.speedup, but only for gas and reaction heat values. 64x means
        ///     gases heat up and cool down 64x faster than real life.
        /// </summary>
        public static readonly CVarDef<float> AtmosHeatScale =
            CVarDef.Create("atmos.heat_scale", 8f, CVar.SERVERONLY);

        /// <summary>
        /// Maximum explosion radius for explosions caused by bursting a gas tank ("max caps").
        /// Setting this to zero disables the explosion but still allows the tank to burst and leak.
        /// </summary>
        public static readonly CVarDef<float> AtmosTankFragment =
            CVarDef.Create("atmos.max_explosion_range", 26f, CVar.SERVERONLY);

        /*
         * MIDI instruments
         */

        public static readonly CVarDef<int> MaxMidiEventsPerSecond =
            CVarDef.Create("midi.max_events_per_second", 1000, CVar.REPLICATED | CVar.SERVER);

        public static readonly CVarDef<int> MaxMidiEventsPerBatch =
            CVarDef.Create("midi.max_events_per_batch", 60, CVar.REPLICATED | CVar.SERVER);

        public static readonly CVarDef<int> MaxMidiBatchesDropped =
            CVarDef.Create("midi.max_batches_dropped", 1, CVar.SERVERONLY);

        public static readonly CVarDef<int> MaxMidiLaggedBatches =
            CVarDef.Create("midi.max_lagged_batches", 8, CVar.SERVERONLY);

        /*
         * Holidays
         */

        public static readonly CVarDef<bool> HolidaysEnabled = CVarDef.Create("holidays.enabled", true, CVar.SERVERONLY);

        /*
         * Branding stuff
         */

        public static readonly CVarDef<bool> BrandingSteam = CVarDef.Create("branding.steam", false, CVar.CLIENTONLY);

        /*
         * OOC
         */

        public static readonly CVarDef<bool> OocEnabled = CVarDef.Create("ooc.enabled", true, CVar.NOTIFY | CVar.REPLICATED);

        public static readonly CVarDef<bool> AdminOocEnabled =
            CVarDef.Create("ooc.enabled_admin", true, CVar.NOTIFY);

        /// <summary>
        /// If true, whenever OOC is disabled the Discord OOC relay will also be disabled.
        /// </summary>
        public static readonly CVarDef<bool> DisablingOOCDisablesRelay = CVarDef.Create("ooc.disabling_ooc_disables_relay", true, CVar.SERVERONLY);

        /// <summary>
        /// Whether or not OOC chat should be enabled during a round.
        /// </summary>
        public static readonly CVarDef<bool> OocEnableDuringRound =
            CVarDef.Create("ooc.enable_during_round", false, CVar.NOTIFY | CVar.REPLICATED | CVar.SERVER);

        public static readonly CVarDef<bool> ShowOocPatronColor =
            CVarDef.Create("ooc.show_ooc_patron_color", true, CVar.ARCHIVE | CVar.REPLICATED | CVar.CLIENT);

        /*
         * LOOC
         */

        public static readonly CVarDef<bool> LoocEnabled = CVarDef.Create("looc.enabled", true, CVar.NOTIFY | CVar.REPLICATED);

        public static readonly CVarDef<bool> AdminLoocEnabled =
            CVarDef.Create("looc.enabled_admin", true, CVar.NOTIFY);

        /// <summary>
        /// True: Dead players can use LOOC
        /// False: Dead player LOOC gets redirected to dead chat
        /// </summary>
        public static readonly CVarDef<bool> DeadLoocEnabled = CVarDef.Create("looc.enabled_dead", false, CVar.NOTIFY | CVar.REPLICATED);

        /// <summary>
        /// True: Crit players can use LOOC
        /// False: Crit player LOOC gets redirected to dead chat
        /// </summary>
        public static readonly CVarDef<bool> CritLoocEnabled = CVarDef.Create("looc.enabled_crit", false, CVar.NOTIFY | CVar.REPLICATED);

        /*
         * Entity Menu Grouping Types
         */
        public static readonly CVarDef<int> EntityMenuGroupingType = CVarDef.Create("entity_menu", 0, CVar.CLIENTONLY);

        /*
         * Whitelist
         */

        /// <summary>
        ///     Controls whether the server will deny any players that are not whitelisted in the DB.
        /// </summary>
        public static readonly CVarDef<bool> WhitelistEnabled =
            CVarDef.Create("whitelist.enabled", false, CVar.SERVERONLY);
        /// <summary>
        ///     Specifies the whitelist prototypes to be used by the server. This should be a comma-separated list of prototypes.
        ///     If a whitelists conditions to be active fail (for example player count), the next whitelist will be used instead. If no whitelist is valid, the player will be allowed to connect.
        /// </summary>
        public static readonly CVarDef<string> WhitelistPrototypeList =
            CVarDef.Create("whitelist.prototype_list", "basicWhitelist", CVar.SERVERONLY);

        /*
         * VOTE
         */

        /// <summary>
        ///     Allows enabling/disabling player-started votes for ultimate authority
        /// </summary>
        public static readonly CVarDef<bool> VoteEnabled =
            CVarDef.Create("vote.enabled", true, CVar.SERVERONLY);

        /// <summary>
        ///     See vote.enabled, but specific to restart votes
        /// </summary>
        public static readonly CVarDef<bool> VoteRestartEnabled =
            CVarDef.Create("vote.restart_enabled", true, CVar.SERVERONLY);

        /// <summary>
        ///     Config for when the restart vote should be allowed to be called regardless with less than this amount of players.
        /// </summary>
        public static readonly CVarDef<int> VoteRestartMaxPlayers =
            CVarDef.Create("vote.restart_max_players", 20, CVar.SERVERONLY);

        /// <summary>
        ///     Config for when the restart vote should be allowed to be called based on percentage of ghosts.
        ///
        public static readonly CVarDef<int> VoteRestartGhostPercentage =
            CVarDef.Create("vote.restart_ghost_percentage", 55, CVar.SERVERONLY);

        /// <summary>
        ///     See vote.enabled, but specific to preset votes
        /// </summary>
        public static readonly CVarDef<bool> VotePresetEnabled =
            CVarDef.Create("vote.preset_enabled", true, CVar.SERVERONLY);

        /// <summary>
        ///     See vote.enabled, but specific to map votes
        /// </summary>
        public static readonly CVarDef<bool> VoteMapEnabled =
            CVarDef.Create("vote.map_enabled", false, CVar.SERVERONLY);

        /// <summary>
        ///     The required ratio of the server that must agree for a restart round vote to go through.
        /// </summary>
        public static readonly CVarDef<float> VoteRestartRequiredRatio =
            CVarDef.Create("vote.restart_required_ratio", 0.85f, CVar.SERVERONLY);

        /// <summary>
        /// Whether or not to prevent the restart vote from having any effect when there is an online admin
        /// </summary>
        public static readonly CVarDef<bool> VoteRestartNotAllowedWhenAdminOnline =
            CVarDef.Create("vote.restart_not_allowed_when_admin_online", true, CVar.SERVERONLY);

        /// <summary>
        ///     The delay which two votes of the same type are allowed to be made by separate people, in seconds.
        /// </summary>
        public static readonly CVarDef<float> VoteSameTypeTimeout =
            CVarDef.Create("vote.same_type_timeout", 240f, CVar.SERVERONLY);

        /// <summary>
        ///     Sets the duration of the map vote timer.
        /// </summary>
        public static readonly CVarDef<int>
            VoteTimerMap = CVarDef.Create("vote.timermap", 90, CVar.SERVERONLY);

        /// <summary>
        ///     Sets the duration of the restart vote timer.
        /// </summary>
        public static readonly CVarDef<int>
            VoteTimerRestart = CVarDef.Create("vote.timerrestart", 60, CVar.SERVERONLY);

        /// <summary>
        ///     Sets the duration of the gamemode/preset vote timer.
        /// </summary>
        public static readonly CVarDef<int>
            VoteTimerPreset = CVarDef.Create("vote.timerpreset", 30, CVar.SERVERONLY);

        /// <summary>
        ///     Sets the duration of the map vote timer when ALONE.
        /// </summary>
        public static readonly CVarDef<int>
            VoteTimerAlone = CVarDef.Create("vote.timeralone", 10, CVar.SERVERONLY);

        /*
         * VOTEKICK
         */

        /// <summary>
        ///     Allows enabling/disabling player-started votekick for ultimate authority
        /// </summary>
        public static readonly CVarDef<bool> VotekickEnabled =
            CVarDef.Create("votekick.enabled", true, CVar.SERVERONLY);

        /// <summary>
        ///     Config for when the votekick should be allowed to be called based on number of eligible voters.
        /// </summary>
        public static readonly CVarDef<int> VotekickEligibleNumberRequirement =
            CVarDef.Create("votekick.eligible_number", 10, CVar.SERVERONLY);

        /// <summary>
        ///     Whether a votekick initiator must be a ghost or not.
        /// </summary>
        public static readonly CVarDef<bool> VotekickInitiatorGhostRequirement =
            CVarDef.Create("votekick.initiator_ghost_requirement", true, CVar.SERVERONLY);

        /// <summary>
        ///     Whether a votekick voter must be a ghost or not.
        /// </summary>
        public static readonly CVarDef<bool> VotekickVoterGhostRequirement =
            CVarDef.Create("votekick.voter_ghost_requirement", true, CVar.SERVERONLY);

        /// <summary>
        ///     Config for how many hours playtime a player must have to be able to vote on a votekick.
        /// </summary>
        public static readonly CVarDef<int> VotekickEligibleVoterPlaytime =
            CVarDef.Create("votekick.voter_playtime", 100, CVar.SERVERONLY);

        /// <summary>
        ///     Config for how many seconds a player must have been dead to initiate a votekick / be able to vote on a votekick.
        /// </summary>
        public static readonly CVarDef<int> VotekickEligibleVoterDeathtime =
            CVarDef.Create("votekick.voter_deathtime", 180, CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        ///     The required ratio of eligible voters that must agree for a votekick to go through.
        /// </summary>
        public static readonly CVarDef<float> VotekickRequiredRatio =
            CVarDef.Create("votekick.required_ratio", 0.6f, CVar.SERVERONLY);

        /// <summary>
        /// Whether or not to prevent the votekick from having any effect when there is an online admin.
        /// </summary>
        public static readonly CVarDef<bool> VotekickNotAllowedWhenAdminOnline =
            CVarDef.Create("votekick.not_allowed_when_admin_online", true, CVar.SERVERONLY);

        /// <summary>
        ///     The delay for which two votekicks are allowed to be made by separate people, in seconds.
        /// </summary>
        public static readonly CVarDef<float> VotekickTimeout =
            CVarDef.Create("votekick.timeout", 120f, CVar.SERVERONLY);

        /// <summary>
        ///     Sets the duration of the votekick vote timer.
        /// </summary>
        public static readonly CVarDef<int>
            VotekickTimer = CVarDef.Create("votekick.timer", 60, CVar.SERVERONLY);

        /// <summary>
        ///     Config for how many hours playtime a player must have to get protection from the Raider votekick type when playing as an antag.
        /// </summary>
        public static readonly CVarDef<int> VotekickAntagRaiderProtection =
            CVarDef.Create("votekick.antag_raider_protection", 10, CVar.SERVERONLY);

        /// <summary>
        ///     Default severity for votekick bans
        /// </summary>
        public static readonly CVarDef<string> VotekickBanDefaultSeverity =
            CVarDef.Create("votekick.ban_default_severity", "High", CVar.ARCHIVE | CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        ///     Duration of a ban caused by a votekick (in minutes).
        /// </summary>
        public static readonly CVarDef<int> VotekickBanDuration =
            CVarDef.Create("votekick.ban_duration", 180, CVar.SERVERONLY);

        /*
         * BAN
         */

        public static readonly CVarDef<bool> BanHardwareIds =
            CVarDef.Create("ban.hardware_ids", true, CVar.SERVERONLY);

        /*
         * Procgen
         */

        /// <summary>
        /// Should we pre-load all of the procgen atlasses.
        /// </summary>
        public static readonly CVarDef<bool> ProcgenPreload =
            CVarDef.Create("procgen.preload", true, CVar.SERVERONLY);

        /*
         * Shuttles
         */

        // Look this is technically eye behavior but its main impact is shuttles so I just dumped it here.
        /// <summary>
        /// If true then the camera will match the grid / map and is unchangeable.
        /// - When traversing grids it will snap to 0 degrees rotation.
        /// False means the player has control over the camera rotation.
        /// - When traversing grids it will snap to the nearest cardinal which will generally be imperceptible.
        /// </summary>
        public static readonly CVarDef<bool> CameraRotationLocked =
            CVarDef.Create("shuttle.camera_rotation_locked", false, CVar.REPLICATED);

        /// <summary>
        /// Whether the arrivals terminal should be on a planet map.
        /// </summary>
        public static readonly CVarDef<bool> ArrivalsPlanet =
            CVarDef.Create("shuttle.arrivals_planet", true, CVar.SERVERONLY);

        /// <summary>
        /// Whether the arrivals shuttle is enabled.
        /// </summary>
        public static readonly CVarDef<bool> ArrivalsShuttles =
            CVarDef.Create("shuttle.arrivals", true, CVar.SERVERONLY);

        /// <summary>
        /// The map to use for the arrivals station.
        /// </summary>
        public static readonly CVarDef<string> ArrivalsMap =
            CVarDef.Create("shuttle.arrivals_map", "/Maps/Misc/terminal.yml", CVar.SERVERONLY);

        /// <summary>
        /// Cooldown between arrivals departures. This should be longer than the FTL time or it will double cycle.
        /// </summary>
        public static readonly CVarDef<float> ArrivalsCooldown =
            CVarDef.Create("shuttle.arrivals_cooldown", 50f, CVar.SERVERONLY);

        /// <summary>
        /// Are players allowed to return on the arrivals shuttle.
        /// </summary>
        public static readonly CVarDef<bool> ArrivalsReturns =
            CVarDef.Create("shuttle.arrivals_returns", false, CVar.SERVERONLY);

        /// <summary>
        /// Should all players who spawn at arrivals have godmode until they leave the map?
        /// </summary>
        public static readonly CVarDef<bool> GodmodeArrivals =
            CVarDef.Create("shuttle.godmode_arrivals", false, CVar.SERVERONLY);

        /// <summary>
        /// If a grid is split then hide any smaller ones under this mass (kg) from the map.
        /// This is useful to avoid split grids spamming out labels.
        /// </summary>
        public static readonly CVarDef<int> HideSplitGridsUnder =
            CVarDef.Create("shuttle.hide_split_grids_under", 30, CVar.SERVERONLY);

        /// <summary>
        /// Whether to automatically spawn escape shuttles.
        /// </summary>
        public static readonly CVarDef<bool> GridFill =
            CVarDef.Create("shuttle.grid_fill", true, CVar.SERVERONLY);

        /// <summary>
        /// Whether to automatically preloading grids by GridPreloaderSystem
        /// </summary>
        public static readonly CVarDef<bool> PreloadGrids =
            CVarDef.Create("shuttle.preload_grids", true, CVar.SERVERONLY);

        /// <summary>
        /// How long the warmup time before FTL start should be.
        /// </summary>
        public static readonly CVarDef<float> FTLStartupTime =
            CVarDef.Create("shuttle.startup_time", 5.5f, CVar.SERVERONLY);

        /// <summary>
        /// How long a shuttle spends in FTL.
        /// </summary>
        public static readonly CVarDef<float> FTLTravelTime =
            CVarDef.Create("shuttle.travel_time", 20f, CVar.SERVERONLY);

        /// <summary>
        /// How long the final stage of FTL before arrival should be.
        /// </summary>
        public static readonly CVarDef<float> FTLArrivalTime =
            CVarDef.Create("shuttle.arrival_time", 5f, CVar.SERVERONLY);

        /// <summary>
        /// How much time needs to pass before a shuttle can FTL again.
        /// </summary>
        public static readonly CVarDef<float> FTLCooldown =
            CVarDef.Create("shuttle.cooldown", 10f, CVar.SERVERONLY);

        /// <summary>
        /// The maximum <see cref="PhysicsComponent.Mass"/> a grid can have before it becomes unable to FTL.
        /// Any value equal to or less than zero will disable this check.
        /// </summary>
        public static readonly CVarDef<float> FTLMassLimit =
            CVarDef.Create("shuttle.mass_limit", 300f, CVar.SERVERONLY);

        /// <summary>
        /// How long to knock down entities for if they aren't buckled when FTL starts and stops.
        /// </summary>
        public static readonly CVarDef<float> HyperspaceKnockdownTime =
            CVarDef.Create("shuttle.hyperspace_knockdown_time", 5f, CVar.SERVERONLY);

        /*
         * Emergency
         */

        /// <summary>
        /// Is the emergency shuttle allowed to be early launched.
        /// </summary>
        public static readonly CVarDef<bool> EmergencyEarlyLaunchAllowed =
            CVarDef.Create("shuttle.emergency_early_launch_allowed", true, CVar.SERVERONLY); //ADT-Tweak - включен ранний запуск аварийного шаттла командованием

        /// <summary>
        /// How long the emergency shuttle remains docked with the station, in seconds.
        /// </summary>
        public static readonly CVarDef<float> EmergencyShuttleDockTime =
            CVarDef.Create("shuttle.emergency_dock_time", 300f, CVar.SERVERONLY); //ADT-Tweak - время стыковки эвакшаттла увеличен до 5 минут

        /// <summary>
        /// If the emergency shuttle can't dock at a priority port, the dock time will be multiplied with this value.
        /// </summary>
        public static readonly CVarDef<float> EmergencyShuttleDockTimeMultiplierOtherDock =
            CVarDef.Create("shuttle.emergency_dock_time_multiplier_other_dock", 1.6667f, CVar.SERVERONLY);

        /// <summary>
        /// If the emergency shuttle can't dock at all, the dock time will be multiplied with this value.
        /// </summary>
        public static readonly CVarDef<float> EmergencyShuttleDockTimeMultiplierNoDock =
            CVarDef.Create("shuttle.emergency_dock_time_multiplier_no_dock", 2f, CVar.SERVERONLY);

        /// <summary>
        /// How long after the console is authorized for the shuttle to early launch.
        /// </summary>
        public static readonly CVarDef<float> EmergencyShuttleAuthorizeTime =
            CVarDef.Create("shuttle.emergency_authorize_time", 30f, CVar.SERVERONLY); //ADT-Tweak - предупреждение о запуске за 30 секунд до отправки

        /// <summary>
        /// The minimum time for the emergency shuttle to arrive at centcomm.
        /// Actual minimum travel time cannot be less than <see cref="ShuttleSystem.DefaultArrivalTime"/>
        /// </summary>
        public static readonly CVarDef<float> EmergencyShuttleMinTransitTime =
            CVarDef.Create("shuttle.emergency_transit_time_min", 60f, CVar.SERVERONLY);

        /// <summary>
        /// The maximum time for the emergency shuttle to arrive at centcomm.
        /// </summary>
        public static readonly CVarDef<float> EmergencyShuttleMaxTransitTime =
            CVarDef.Create("shuttle.emergency_transit_time_max", 180f, CVar.SERVERONLY);

        /// <summary>
        /// Whether the emergency shuttle is enabled or should the round just end.
        /// </summary>
        public static readonly CVarDef<bool> EmergencyShuttleEnabled =
            CVarDef.Create("shuttle.emergency", true, CVar.SERVERONLY);

        /// <summary>
        ///     The percentage of time passed from the initial call to when the shuttle can no longer be recalled.
        ///     ex. a call time of 10min and turning point of 0.5 means the shuttle cannot be recalled after 5 minutes.
        /// </summary>
        public static readonly CVarDef<float> EmergencyRecallTurningPoint =
            CVarDef.Create("shuttle.recall_turning_point", 0.5f, CVar.SERVERONLY);

        /// <summary>
        ///     Time in minutes after round start to auto-call the shuttle. Set to zero to disable.
        /// </summary>
        public static readonly CVarDef<int> EmergencyShuttleAutoCallTime =
            CVarDef.Create("shuttle.auto_call_time", 270, CVar.SERVERONLY); //ADT-Tweak - автоматический эвак вызывается после 3 часов

        /// <summary>
        ///     Time in minutes after the round was extended (by recalling the shuttle) to call
        ///     the shuttle again.
        /// </summary>
        public static readonly CVarDef<int> EmergencyShuttleAutoCallExtensionTime =
            CVarDef.Create("shuttle.auto_call_extension_time", 45, CVar.SERVERONLY);

        /*
         * Crew Manifests
         */

        /// <summary>
        ///     Setting this allows a crew manifest to be opened from any window
        ///     that has a crew manifest button, and sends the correct message.
        ///     If this is false, only in-game entities will allow you to see
        ///     the crew manifest, if the functionality is coded in.
        ///     Having administrator priveledge ignores this, but will still
        ///     hide the button in UI windows.
        /// </summary>
        public static readonly CVarDef<bool> CrewManifestWithoutEntity =
            CVarDef.Create("crewmanifest.no_entity", true, CVar.REPLICATED);

        /// <summary>
        ///     Setting this allows the crew manifest to be viewed from 'unsecure'
        ///     entities, such as the PDA.
        /// </summary>
        public static readonly CVarDef<bool> CrewManifestUnsecure =
            CVarDef.Create("crewmanifest.unsecure", true, CVar.REPLICATED);

        /*
         * Biomass
         */

        /// <summary>
        ///     Enabled: Cloning has 70% cost and reclaimer will refuse to reclaim corpses with souls. (For LRP).
        ///     Disabled: Cloning has full biomass cost and reclaimer can reclaim corpses with souls. (Playtested and balanced for MRP+).
        /// </summary>
        public static readonly CVarDef<bool> BiomassEasyMode =
            CVarDef.Create("biomass.easy_mode", true, CVar.SERVERONLY);

        /*
         * Anomaly
         */

        /// <summary>
        ///     A scale factor applied to a grid's bounds when trying to find a spot to randomly generate an anomaly.
        /// </summary>
        public static readonly CVarDef<float> AnomalyGenerationGridBoundsScale =
            CVarDef.Create("anomaly.generation_grid_bounds_scale", 0.6f, CVar.SERVERONLY);

        /*
         * VIEWPORT
         */

        public static readonly CVarDef<bool> ViewportStretch =
            CVarDef.Create("viewport.stretch", true, CVar.CLIENTONLY | CVar.ARCHIVE);

        public static readonly CVarDef<int> ViewportFixedScaleFactor =
            CVarDef.Create("viewport.fixed_scale_factor", 2, CVar.CLIENTONLY | CVar.ARCHIVE);

        // This default is basically specifically chosen so fullscreen/maximized 1080p hits a 2x snap and does NN.
        public static readonly CVarDef<int> ViewportSnapToleranceMargin =
            CVarDef.Create("viewport.snap_tolerance_margin", 64, CVar.CLIENTONLY | CVar.ARCHIVE);

        public static readonly CVarDef<int> ViewportSnapToleranceClip =
            CVarDef.Create("viewport.snap_tolerance_clip", 32, CVar.CLIENTONLY | CVar.ARCHIVE);

        public static readonly CVarDef<bool> ViewportScaleRender =
            CVarDef.Create("viewport.scale_render", true, CVar.CLIENTONLY | CVar.ARCHIVE);

        public static readonly CVarDef<int> ViewportMinimumWidth =
            CVarDef.Create("viewport.minimum_width", 15, CVar.REPLICATED | CVar.SERVER);

        public static readonly CVarDef<int> ViewportMaximumWidth =
            CVarDef.Create("viewport.maximum_width", 29, CVar.REPLICATED | CVar.SERVER); //ADT 16:9

        public static readonly CVarDef<int> ViewportWidth =
            CVarDef.Create("viewport.width", 29, CVar.CLIENTONLY | CVar.ARCHIVE); //ADT 16:9

        public static readonly CVarDef<bool> ViewportVerticalFit =
            CVarDef.Create("viewport.vertical_fit", true, CVar.CLIENTONLY | CVar.ARCHIVE);

        /*
         * UI
         */

        public static readonly CVarDef<string> UILayout =
            CVarDef.Create("ui.layout", "Default", CVar.CLIENTONLY | CVar.ARCHIVE);

        public static readonly CVarDef<string> DefaultScreenChatSize =
            CVarDef.Create("ui.default_chat_size", "", CVar.CLIENTONLY | CVar.ARCHIVE);

        public static readonly CVarDef<string> SeparatedScreenChatSize =
            CVarDef.Create("ui.separated_chat_size", "0.6,0", CVar.CLIENTONLY | CVar.ARCHIVE);


        /*
        * Accessibility
        */

        /// <summary>
        /// Chat window opacity slider, controlling the alpha of the chat window background.
        /// Goes from to 0 (completely transparent) to 1 (completely opaque)
        /// </summary>
        public static readonly CVarDef<float> ChatWindowOpacity =
            CVarDef.Create("accessibility.chat_window_transparency", 0.85f, CVar.CLIENTONLY | CVar.ARCHIVE);

        /// <summary>
        /// Toggle for visual effects that may potentially cause motion sickness.
        /// Where reasonable, effects affected by this CVar should use an alternate effect.
        /// Please do not use this CVar as a bandaid for effects that could otherwise be made accessible without issue.
        /// </summary>
        public static readonly CVarDef<bool> ReducedMotion =
            CVarDef.Create("accessibility.reduced_motion", false, CVar.CLIENTONLY | CVar.ARCHIVE);

        public static readonly CVarDef<bool> ChatEnableColorName =
            CVarDef.Create("accessibility.enable_color_name", true, CVar.CLIENTONLY | CVar.ARCHIVE, "Toggles displaying names with individual colors.");

        /// <summary>
        /// Screen shake intensity slider, controlling the intensity of the CameraRecoilSystem.
        /// Goes from 0 (no recoil at all) to 1 (regular amounts of recoil)
        /// </summary>
        public static readonly CVarDef<float> ScreenShakeIntensity =
            CVarDef.Create("accessibility.screen_shake_intensity", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);

        /// <summary>
        /// A generic toggle for various visual effects that are color sensitive.
        /// As of 2/16/24, only applies to progress bar colors.
        /// </summary>
        public static readonly CVarDef<bool> AccessibilityColorblindFriendly =
            CVarDef.Create("accessibility.colorblind_friendly", false, CVar.CLIENTONLY | CVar.ARCHIVE);

        /*
         * CHAT
         */

        /// <summary>
        /// Chat rate limit values are accounted in periods of this size (seconds).
        /// After the period has passed, the count resets.
        /// </summary>
        /// <seealso cref="ChatRateLimitCount"/>
        public static readonly CVarDef<float> ChatRateLimitPeriod =
            CVarDef.Create("chat.rate_limit_period", 2f, CVar.SERVERONLY);

        /// <summary>
        /// How many chat messages are allowed in a single rate limit period.
        /// </summary>
        /// <remarks>
        /// The total rate limit throughput per second is effectively
        /// <see cref="ChatRateLimitCount"/> divided by <see cref="ChatRateLimitCount"/>.
        /// </remarks>
        /// <seealso cref="ChatRateLimitPeriod"/>
        public static readonly CVarDef<int> ChatRateLimitCount =
            CVarDef.Create("chat.rate_limit_count", 10, CVar.SERVERONLY);

        /// <summary>
        /// Minimum delay (in seconds) between notifying admins about chat message rate limit violations.
        /// A negative value disables admin announcements.
        /// </summary>
        public static readonly CVarDef<int> ChatRateLimitAnnounceAdminsDelay =
            CVarDef.Create("chat.rate_limit_announce_admins_delay", 15, CVar.SERVERONLY);

        public static readonly CVarDef<int> ChatMaxMessageLength =
            CVarDef.Create("chat.max_message_length", 1000, CVar.SERVER | CVar.REPLICATED);

        public static readonly CVarDef<int> ChatMaxAnnouncementLength =
            CVarDef.Create("chat.max_announcement_length", 700, CVar.SERVER | CVar.REPLICATED);// ADT Tweak 256

        public static readonly CVarDef<bool> ChatSanitizerEnabled =
            CVarDef.Create("chat.chat_sanitizer_enabled", true, CVar.SERVERONLY);

        public static readonly CVarDef<bool> ChatShowTypingIndicator =
            CVarDef.Create("chat.show_typing_indicator", true, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

        public static readonly CVarDef<bool> ChatEnableFancyBubbles =
            CVarDef.Create("chat.enable_fancy_bubbles", true, CVar.CLIENTONLY | CVar.ARCHIVE, "Toggles displaying fancy speech bubbles, which display the speaking character's name.");

        public static readonly CVarDef<bool> ChatFancyNameBackground =
            CVarDef.Create("chat.fancy_name_background", false, CVar.CLIENTONLY | CVar.ARCHIVE, "Toggles displaying a background under the speaking character's name.");

        /// <summary>
        /// A message broadcast to each player that joins the lobby.
        /// May be changed by admins ingame through use of the "set-motd" command.
        /// In this case the new value, if not empty, is broadcast to all connected players and saved between rounds.
        /// May be requested by any player through use of the "get-motd" command.
        /// </summary>
        public static readonly CVarDef<string> MOTD =
            CVarDef.Create("chat.motd", "", CVar.SERVER | CVar.SERVERONLY | CVar.ARCHIVE, "A message broadcast to each player that joins the lobby.");

        /*
         * AFK
         */

        /// <summary>
        /// How long a client can go without any input before being considered AFK.
        /// </summary>
        public static readonly CVarDef<float> AfkTime =
            CVarDef.Create("afk.time", 60f, CVar.SERVERONLY);

        /*
         * IC
         */

        /// <summary>
        /// Restricts IC character names to alphanumeric chars.
        /// </summary>
        public static readonly CVarDef<bool> RestrictedNames =
            CVarDef.Create("ic.restricted_names", true, CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Allows flavor text (character descriptions)
        /// </summary>
        public static readonly CVarDef<bool> FlavorText =
            CVarDef.Create("ic.flavor_text", false, CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Adds a period at the end of a sentence if the sentence ends in a letter.
        /// </summary>
        public static readonly CVarDef<bool> ChatPunctuation =
            CVarDef.Create("ic.punctuation", false, CVar.SERVER);

        /// <summary>
        /// Enables automatically forcing IC name rules. Uppercases the first letter of the first and last words of the name
        /// </summary>
        public static readonly CVarDef<bool> ICNameCase =
            CVarDef.Create("ic.name_case", true, CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Whether or not players' characters are randomly generated rather than using their selected characters in the creator.
        /// </summary>
        public static readonly CVarDef<bool> ICRandomCharacters =
            CVarDef.Create("ic.random_characters", false, CVar.SERVER);

        /// <summary>
        /// A weighted random prototype used to determine the species selected for random characters.
        /// </summary>
        public static readonly CVarDef<string> ICRandomSpeciesWeights =
            CVarDef.Create("ic.random_species_weights", "SpeciesWeights", CVar.SERVER);

        /// <summary>
        /// Control displaying SSD indicators near players
        /// </summary>
        public static readonly CVarDef<bool> ICShowSSDIndicator =
            CVarDef.Create("ic.show_ssd_indicator", true, CVar.CLIENTONLY);

        /*
         * Salvage
         */

        /// <summary>
        /// Duration for missions
        /// </summary>
        public static readonly CVarDef<float>
            SalvageExpeditionDuration = CVarDef.Create("salvage.expedition_duration", 660f, CVar.REPLICATED);

        /// <summary>
        /// Cooldown for missions.
        /// </summary>
        public static readonly CVarDef<float>
            SalvageExpeditionCooldown = CVarDef.Create("salvage.expedition_cooldown", 780f, CVar.REPLICATED);

        /*
         * Flavor
         */

        /// <summary>
        ///     Flavor limit. This is to ensure that having a large mass of flavors in
        ///     some food object won't spam a user with flavors.
        /// </summary>
        public static readonly CVarDef<int>
            FlavorLimit = CVarDef.Create("flavor.limit", 10, CVar.SERVERONLY);

        /*
         * Mapping
         */

        /// <summary>
        ///     Will mapping mode enable autosaves when it's activated?
        /// </summary>
        public static readonly CVarDef<bool>
            AutosaveEnabled = CVarDef.Create("mapping.autosave", true, CVar.SERVERONLY);

        /// <summary>
        ///     Autosave interval in seconds.
        /// </summary>
        public static readonly CVarDef<float>
            AutosaveInterval = CVarDef.Create("mapping.autosave_interval", 600f, CVar.SERVERONLY);

        /// <summary>
        ///     Directory in server user data to save to. Saves will be inside folders in this directory.
        /// </summary>
        public static readonly CVarDef<string>
            AutosaveDirectory = CVarDef.Create("mapping.autosave_dir", "Autosaves", CVar.SERVERONLY);


        /*
         * Rules
         */

        /// <summary>
        /// Time that players have to wait before rules can be accepted.
        /// </summary>
        public static readonly CVarDef<float> RulesWaitTime =
            CVarDef.Create("rules.time", 45f, CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Don't show rules to localhost/loopback interface.
        /// </summary>
        public static readonly CVarDef<bool> RulesExemptLocal =
            CVarDef.Create("rules.exempt_local", true, CVar.SERVERONLY);


        /*
         * Autogeneration
         */

        public static readonly CVarDef<string> DestinationFile =
            CVarDef.Create("autogen.destination_file", "", CVar.SERVER | CVar.SERVERONLY);

        /*
         * Network Resource Manager
         */

        /// <summary>
        /// Whether uploaded files will be stored in the server's database.
        /// This is useful to keep "logs" on what files admins have uploaded in the past.
        /// </summary>
        public static readonly CVarDef<bool> ResourceUploadingStoreEnabled =
            CVarDef.Create("netres.store_enabled", true, CVar.SERVER | CVar.SERVERONLY);

        /// <summary>
        /// Numbers of days before stored uploaded files are deleted. Set to zero or negative to disable auto-delete.
        /// This is useful to free some space automatically. Auto-deletion runs only on server boot.
        /// </summary>
        public static readonly CVarDef<int> ResourceUploadingStoreDeletionDays =
            CVarDef.Create("netres.store_deletion_days", 30, CVar.SERVER | CVar.SERVERONLY);

        /*
         * Controls
         */

        /// <summary>
        /// Deadzone for drag-drop interactions.
        /// </summary>
        public static readonly CVarDef<float> DragDropDeadZone =
            CVarDef.Create("control.drag_dead_zone", 12f, CVar.CLIENTONLY | CVar.ARCHIVE);

        /// <summary>
        /// Toggles whether the walking key is a toggle or a held key.
        /// </summary>
        public static readonly CVarDef<bool> ToggleWalk =
            CVarDef.Create("control.toggle_walk", false, CVar.CLIENTONLY | CVar.ARCHIVE);

        /*
         * Interactions
         */

        // The rationale behind the default limit is simply that I can easily get to 7 interactions per second by just
        // trying to spam toggle a light switch or lever (though the UseDelay component limits the actual effect of the
        // interaction).  I don't want to accidentally spam admins with alerts just because somebody is spamming a
        // key manually, nor do we want to alert them just because the player is having network issues and the server
        // receives multiple interactions at once. But we also want to try catch people with modified clients that spam
        // many interactions on the same tick. Hence, a very short period, with a relatively high count.

        /// <summary>
        /// Maximum number of interactions that a player can perform within <see cref="InteractionRateLimitCount"/> seconds
        /// </summary>
        public static readonly CVarDef<int> InteractionRateLimitCount =
            CVarDef.Create("interaction.rate_limit_count", 5, CVar.SERVER | CVar.REPLICATED);

        /// <seealso cref="InteractionRateLimitCount"/>
        public static readonly CVarDef<float> InteractionRateLimitPeriod =
            CVarDef.Create("interaction.rate_limit_period", 0.5f, CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Minimum delay (in seconds) between notifying admins about interaction rate limit violations. A negative
        /// value disables admin announcements.
        /// </summary>
        public static readonly CVarDef<int> InteractionRateLimitAnnounceAdminsDelay =
            CVarDef.Create("interaction.rate_limit_announce_admins_delay", 120, CVar.SERVERONLY);

        /*
         * STORAGE
         */

        /// <summary>
        /// Whether or not the storage UI is static and bound to the hotbar, or unbound and allowed to be dragged anywhere.
        /// </summary>
        public static readonly CVarDef<bool> StaticStorageUI =
            CVarDef.Create("control.static_storage_ui", true, CVar.CLIENTONLY | CVar.ARCHIVE);

        /// <summary>
        /// Whether or not the storage window uses a transparent or opaque sprite.
        /// </summary>
        public static readonly CVarDef<bool> OpaqueStorageWindow =
            CVarDef.Create("control.opaque_storage_background", false, CVar.CLIENTONLY | CVar.ARCHIVE);

        /*
         * UPDATE
         */

        /// <summary>
        /// If a server update restart is pending, the delay after the last player leaves before we actually restart. In seconds.
        /// </summary>
        public static readonly CVarDef<float> UpdateRestartDelay =
            CVarDef.Create("update.restart_delay", 20f, CVar.SERVERONLY);

        /*
         * Ghost
         */

        /// <summary>
        /// The time you must spend reading the rules, before the "Request" button is enabled
        /// </summary>
        public static readonly CVarDef<float> GhostRoleTime =
            CVarDef.Create("ghost.role_time", 3f, CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        /// If ghost role lotteries should be made near-instanteous.
        /// </summary>
        public static readonly CVarDef<bool> GhostQuickLottery =
            CVarDef.Create("ghost.quick_lottery", false, CVar.SERVERONLY);

        /// <summary>
        /// Whether or not to kill the player's mob on ghosting, when it is in a critical health state.
        /// </summary>
        public static readonly CVarDef<bool> GhostKillCrit =
            CVarDef.Create("ghost.kill_crit", true, CVar.REPLICATED | CVar.SERVER);

        /*
         * Fire alarm
         */

        /// <summary>
        ///     If fire alarms should have all access, or if activating/resetting these
        ///     should be restricted to what is dictated on a player's access card.
        ///     Defaults to true.
        /// </summary>
        public static readonly CVarDef<bool> FireAlarmAllAccess =
            CVarDef.Create("firealarm.allaccess", true, CVar.SERVERONLY);

        /*
         * PLAYTIME
         */

        /// <summary>
        /// Time between play time autosaves, in seconds.
        /// </summary>
        public static readonly CVarDef<float>
            PlayTimeSaveInterval = CVarDef.Create("playtime.save_interval", 900f, CVar.SERVERONLY);

        /*
         * INFOLINKS
         */

        /// <summary>
        /// Link to Discord server to show in the launcher.
        /// </summary>
        public static readonly CVarDef<string> InfoLinksDiscord =
            CVarDef.Create("infolinks.discord", "", CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Link to website to show in the launcher.
        /// </summary>
        public static readonly CVarDef<string> InfoLinksForum =
            CVarDef.Create("infolinks.forum", "", CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Link to GitHub page to show in the launcher.
        /// </summary>
        public static readonly CVarDef<string> InfoLinksGithub =
            CVarDef.Create("infolinks.github", "", CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Link to website to show in the launcher.
        /// </summary>
        public static readonly CVarDef<string> InfoLinksWebsite =
            CVarDef.Create("infolinks.website", "", CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Link to wiki to show in the launcher.
        /// </summary>
        public static readonly CVarDef<string> InfoLinksWiki =
            CVarDef.Create("infolinks.wiki", "", CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Link to Patreon. Not shown in the launcher currently.
        /// </summary>
        public static readonly CVarDef<string> InfoLinksPatreon =
            CVarDef.Create("infolinks.patreon", "", CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Link to the bug report form.
        /// </summary>
        public static readonly CVarDef<string> InfoLinksBugReport =
            CVarDef.Create("infolinks.bug_report", "", CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Link to site handling ban appeals. Shown in ban disconnect messages.
        /// </summary>
        public static readonly CVarDef<string> InfoLinksAppeal =
            CVarDef.Create("infolinks.appeal", "https://discord.com/channels/901772674865455115/1245787985891561544", CVar.SERVER | CVar.REPLICATED); //ADT-Tweak: Ссылка на обжалование

        /*
         * CONFIG
         */

        // These are server-only for now since I don't foresee a client use yet,
        // and I don't wanna have to start coming up with like .client suffixes and stuff like that.

        /// <summary>
        /// Configuration presets to load during startup.
        /// Multiple presets can be separated by comma and are loaded in order.
        /// </summary>
        /// <remarks>
        /// Loaded presets must be located under the <c>ConfigPresets/</c> resource directory and end with the <c>.toml</c> extension.
        /// Only the file name (without extension) must be given for this variable.
        /// </remarks>
        public static readonly CVarDef<string> ConfigPresets =
            CVarDef.Create("config.presets", "", CVar.SERVERONLY);

        /// <summary>
        /// Whether to load the preset development CVars.
        /// This disables some things like lobby to make development easier.
        /// Even when true, these are only loaded if the game is compiled with <c>DEVELOPMENT</c> set.
        /// </summary>
        public static readonly CVarDef<bool> ConfigPresetDevelopment =
            CVarDef.Create("config.preset_development", true, CVar.SERVERONLY);

        /// <summary>
        /// Whether to load the preset debug CVars.
        /// Even when true, these are only loaded if the game is compiled with <c>DEBUG</c> set.
        /// </summary>
        public static readonly CVarDef<bool> ConfigPresetDebug =
            CVarDef.Create("config.preset_debug", true, CVar.SERVERONLY);

        /*
         * World Generation
         */
        /// <summary>
        ///     Whether or not world generation is enabled.
        /// </summary>
        public static readonly CVarDef<bool> WorldgenEnabled =
            CVarDef.Create("worldgen.enabled", false, CVar.SERVERONLY);

        /// <summary>
        ///     The worldgen config to use.
        /// </summary>
        public static readonly CVarDef<string> WorldgenConfig =
            CVarDef.Create("worldgen.worldgen_config", "Default", CVar.SERVERONLY);

        /// <summary>
        ///     The maximum amount of time the entity GC can process, in ms.
        /// </summary>
        public static readonly CVarDef<int> GCMaximumTimeMs =
            CVarDef.Create("entgc.maximum_time_ms", 5, CVar.SERVERONLY);

        /*
         * Replays
         */

        /// <summary>
        ///     Whether or not to record admin chat. If replays are being publicly distributes, this should probably be
        ///     false.
        /// </summary>
        public static readonly CVarDef<bool> ReplayRecordAdminChat =
            CVarDef.Create("replay.record_admin_chat", false, CVar.ARCHIVE);

        /// <summary>
        /// Automatically record full rounds as replays.
        /// </summary>
        public static readonly CVarDef<bool> ReplayAutoRecord =
            CVarDef.Create("replay.auto_record", false, CVar.SERVERONLY);

        /// <summary>
        /// The file name to record automatic replays to. The path is relative to <see cref="CVars.ReplayDirectory"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the path includes slashes, directories will be automatically created if necessary.
        /// </para>
        /// <para>
        /// A number of substitutions can be used to automatically fill in the file name: <c>{year}</c>, <c>{month}</c>, <c>{day}</c>, <c>{hour}</c>, <c>{minute}</c>, <c>{round}</c>.
        /// </para>
        /// </remarks>
        public static readonly CVarDef<string> ReplayAutoRecordName =
            CVarDef.Create("replay.auto_record_name", "{year}_{month}_{day}-{hour}_{minute}-round_{round}.zip", CVar.SERVERONLY);

        /// <summary>
        /// Path that, if provided, automatic replays are initially recorded in.
        /// When the recording is done, the file is moved into its final destination.
        /// Unless this path is rooted, it will be relative to <see cref="CVars.ReplayDirectory"/>.
        /// </summary>
        public static readonly CVarDef<string> ReplayAutoRecordTempDir =
            CVarDef.Create("replay.auto_record_temp_dir", "", CVar.SERVERONLY);

        /*
         * Miscellaneous
         */

        public static readonly CVarDef<bool> GatewayGeneratorEnabled =
            CVarDef.Create("gateway.generator_enabled", true);

        // Clippy!
        public static readonly CVarDef<string> TippyEntity =
            CVarDef.Create("tippy.entity", "Tippy", CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        ///     The number of seconds that must pass for a single entity to be able to point at something again.
        /// </summary>
        public static readonly CVarDef<float> PointingCooldownSeconds =
            CVarDef.Create("pointing.cooldown_seconds", 0.5f, CVar.SERVERONLY);

        /*
         * DEBUG
         */

        /// <summary>
        /// A simple toggle to test <c>OptionsVisualizerComponent</c>.
        /// </summary>
        public static readonly CVarDef<bool> DebugOptionVisualizerTest =
            CVarDef.Create("debug.option_visualizer_test", false, CVar.CLIENTONLY);

        /// <summary>
        /// Set to true to disable parallel processing in the pow3r solver.
        /// </summary>
        public static readonly CVarDef<bool> DebugPow3rDisableParallel =
            CVarDef.Create("debug.pow3r_disable_parallel", true, CVar.SERVERONLY);
    }
/// <summary>
/// Contains all the CVars used by content.
/// </summary>
/// <remarks>
/// NOTICE FOR FORKS: Put your own CVars in a separate file with a different [CVarDefs] attribute. RT will automatically pick up on it.
/// </remarks>
[CVarDefs]
public sealed partial class CCVars : CVars
{
    // Only debug stuff lives here.

    /// <summary>
    /// A simple toggle to test <c>OptionsVisualizerComponent</c>.
    /// </summary>
    public static readonly CVarDef<bool> DebugOptionVisualizerTest =
        CVarDef.Create("debug.option_visualizer_test", false, CVar.CLIENTONLY);

    /// <summary>
    /// Set to true to disable parallel processing in the pow3r solver.
    /// </summary>
    public static readonly CVarDef<bool> DebugPow3rDisableParallel =
        CVarDef.Create("debug.pow3r_disable_parallel", true, CVar.SERVERONLY);
}
