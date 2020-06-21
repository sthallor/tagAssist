Todo: This needs to be custom per rig.

This is for 778
INSERT INTO `tags` (`tag_path`, `cimplicity_tag_path`, `display_name`, `definition`, `units`, `folder`) VALUES
	('Analytics/Real_Time_Rig_state/hole_depth_m', 'Analytics/Real_Time_Rig_state/hole_depth_m', 'Hole Depth', '{}', 'm', NULL),
	('Analytics/Real_Time_Rig_state/bit_depth_m', 'Analytics/Real_Time_Rig_state/bit_depth_m', 'Bit Depth', '{}', 'm', NULL),
	('Analytics/Real_Time_Rig_state/standpipe_pressure_kpa', 'Analytics/Real_Time_Rig_state/standpipe_pressure_kpa', 'Standpipe Pressure', '{}', 'kPa', NULL),
	('Analytics/Real_Time_Rig_state/strokes_per_minute_total', 'Analytics/Real_Time_Rig_state/strokes_per_minute_total', 'MP1 SPM', '{}', 'spm', NULL),
	('Analytics/Real_Time_Rig_state/strokes_per_minute_total', 'Analytics/Real_Time_Rig_state/strokes_per_minute_total', 'MP2 SPM', '{}', 'spm', NULL),
	('Analytics/Real_Time_Rig_state/hookload_dan', 'Analytics/Real_Time_Rig_state/hookload_dan', 'Hookload', '{}', 'DaN', NULL),
	('Analytics/Real_Time_Rig_state/block_height_m', 'Analytics/Real_Time_Rig_state/block_height_m', 'Block Height', '{}', 'm', NULL),
	('Analytics/Real_Time_Rig_state/td_quill_torque_nm', 'Analytics/Real_Time_Rig_state/td_quill_torque_nm', 'Top Drive Torque', '{}', 'Nm', NULL),
	('Analytics/Real_Time_Rig_state/weight_on_bit_pv_dan', 'Analytics/Real_Time_Rig_state/weight_on_bit_pv_dan', 'Weight On Bit', '{}', 'DaN', NULL),
	('Analytics/Real_Time_Rig_state/rop_mps', 'Analytics/Real_Time_Rig_state/rop_mps', 'ROP', '{}', 'mps', NULL),
	('Analytics/Real_Time_Rig_state/td_quill_rpm', 'Analytics/Real_Time_Rig_state/td_quill_rpm', 'Top Drive RPM', '{}', 'RPM', NULL)


These are for 548/549/785

INSERT INTO `tags` (`tag_path`, `cimplicity_tag_path`, `display_name`, `definition`, `units`, `folder`) VALUES
	('Pason/hole_depth_ft', 'Pason/hole_depth_ft', 'Hole Depth', '{}', 'ft', NULL),
	('Pason/bit_depth_ft', 'Pason/bit_depth_ft', 'Bit Depth', '{}', 'ft', NULL),
	('Pason/pressure_psi', 'Pason/pressure_psi', 'Standpipe Pressure', '{}', 'psi', NULL),
	('Pason/strokes_1_spm', 'Pason/strokes_1_spm', 'MP1 SPM', '{}', 'spm', NULL),
	('Pason/strokes_2_spm', 'Pason/strokes_2_spm', 'MP2 SPM', '{}', 'spm', NULL),
	('Pason/hook_load_klbs', 'Pason/hook_load_klbs', 'Hookload', '{}', 'klbs', NULL),
	('Pason/block_ht_ft', 'Pason/block_ht_ft', 'Block Height', '{ft}', 'feet', NULL),
	('Pason/torque_klbs', 'Pason/torque_klbs', 'Top Drive Torque', '{klbs}', 'kNm', NULL),
	('Pason/wt_on_bit_klbs', 'Pason/wt_on_bit_klbs', 'Weight On Bit', '{}', 'klbs', NULL),
	('Pason/rop_mps', 'Pason/rop_mps', 'ROP', '{}', 'mps', NULL),
	('Pason/rotary_rpm', 'Pason/rotary_rpm', 'Top Drive RPM', '{}', 'RPM', NULL)


770
INSERT INTO `tags` VALUES ('local/s1500/Analytics/RealTimeRigState/HOLE_DEPTH_m', 'local/s1500/Analytics/RealTimeRigState/HOLE_DEPTH_m', 'Hole Depth', '{}', 'm', NULL);
INSERT INTO `tags` VALUES ('local/s1500/Analytics/RealTimeRigState/BIT_DEPTH_m', 'local/s1500/Analytics/RealTimeRigState/BIT_DEPTH_m', 'Bit Depth', '{}', 'm', NULL);
INSERT INTO `tags` VALUES ('local/s1500/Analytics/RealTimeRigState/PRESSURE_kPa', 'local/s1500/Analytics/RealTimeRigState/PRESSURE_kPa', 'Standpipe Pressure', '{}', 'kPa', NULL);
INSERT INTO `tags` VALUES ('local/s1500/Analytics/RealTimeRigState/STROKES_1_SPM', 'local/s1500/Analytics/RealTimeRigState/STROKES_1_SPM', 'MP1 SPM', '{}', 'spm', NULL);
INSERT INTO `tags` VALUES ('local/s1500/Analytics/RealTimeRigState/STROKES_2_SPM', 'local/s1500/Analytics/RealTimeRigState/STROKES_2_SPM', 'MP2 SPM', '{}', 'spm', NULL);
INSERT INTO `tags` VALUES ('local/s1500/Analytics/RealTimeRigState/HOOK_LOAD_daN', 'local/s1500/Analytics/RealTimeRigState/HOOK_LOAD_daN', 'Hookload', '{}', 'DaN', NULL);
INSERT INTO `tags` VALUES ('local/s1500/Analytics/RealTimeRigState/BLOCK_HT_m', 'local/s1500/Analytics/RealTimeRigState/BLOCK_HT_m', 'Block Height', '{}', 'm', NULL);
INSERT INTO `tags` VALUES ('local/s1500/Analytics/RealTimeRigState/TORQUE_Nm', 'local/s1500/Analytics/RealTimeRigState/TORQUE_Nm', 'Top Drive Torque', '{}', 'Nm', NULL);
INSERT INTO `tags` VALUES ('local/s1500/Analytics/RealTimeRigState/WT_ON_BIT_daN', 'local/s1500/Analytics/RealTimeRigState/WT_ON_BIT_daN', 'Weight On Bit', '{}', 'DaN', NULL);
INSERT INTO `tags` VALUES ('local/s1500/Analytics/RealTimeRigState/ROP_mps', 'local/s1500/Analytics/RealTimeRigState/ROP_mps', 'ROP', '{}', 'mps', NULL);
INSERT INTO `tags` VALUES ('local/s1500/Analytics/RealTimeRigState/ROTARY_RPM', 'local/s1500/Analytics/RealTimeRigState/ROTARY_RPM', 'Top Drive RPM', '{}', 'RPM', NULL);


These are for 778
INSERT INTO `tags` VALUES ('s1500/Pason/pason_EDR_iomap/HOLE_DEPTH_m', 's1500/Pason/pason_EDR_iomap/HOLE_DEPTH_m', 'Hole Depth', '{}', 'm', NULL);
INSERT INTO `tags` VALUES ('s1500/Pason/pason_EDR_iomap/BIT_DEPTH_m', 's1500/Pason/pason_EDR_iomap/BIT_DEPTH_m', 'Bit Depth', '{}', 'm', NULL);
INSERT INTO `tags` VALUES ('s1500/Pason/pason_EDR_iomap/PRESSURE_kPa', 's1500/Pason/pason_EDR_iomap/PRESSURE_kPa', 'Standpipe Pressure', '{}', 'kPa', NULL);
INSERT INTO `tags` VALUES ('s1500/Pason/pason_EDR_iomap/STROKES_1_SPM', 's1500/Pason/pason_EDR_iomap/STROKES_1_SPM', 'MP1 SPM', '{}', 'spm', NULL);
INSERT INTO `tags` VALUES ('s1500/Pason/pason_EDR_iomap/STROKES_2_SPM', 's1500/Pason/pason_EDR_iomap/STROKES_2_SPM', 'MP2 SPM', '{}', 'spm', NULL);
INSERT INTO `tags` VALUES ('s1500/Pason/pason_EDR_iomap/HOOK_LOAD_daN', 's1500/Pason/pason_EDR_iomap/HOOK_LOAD_daN', 'Hookload', '{}', 'DaN', NULL);
INSERT INTO `tags` VALUES ('s1500/Pason/pason_EDR_iomap/BLOCK_HT_m', 's1500/Pason/pason_EDR_iomap/BLOCK_HT_m', 'Block Height', '{}', 'm', NULL);
INSERT INTO `tags` VALUES ('s1500/Pason/pason_EDR_iomap/torque_Nm', 's1500/Pason/pason_EDR_iomap/torque_Nm', 'Top Drive Torque', '{}', 'Nm', NULL);
INSERT INTO `tags` VALUES ('s1500/Pason/pason_EDR_iomap/WT_ON_BIT_daN', 's1500/Pason/pason_EDR_iomap/WT_ON_BIT_daN', 'Weight On Bit', '{}', 'DaN', NULL);
INSERT INTO `tags` VALUES ('s1500/Pason/pason_EDR_iomap/ROP_mps', 's1500/Pason/pason_EDR_iomap/ROP_mps', 'ROP', '{}', 'mps', NULL);
INSERT INTO `tags` VALUES ('s1500/Pason/pason_EDR_iomap/ROTARY_RPM', 's1500/Pason/pason_EDR_iomap/ROTARY_RPM', 'Top Drive RPM', '{}', 'RPM', NULL);


INSERT INTO `tags` VALUES ('Local/s1500/Pason/pason_EDR_iomap/PASON_HOLE_DEPTH_ft', 'Local/s1500/Pason/pason_EDR_iomap/PASON_HOLE_DEPTH_ft', 'Hole Depth', '{}', 'ft', NULL);
INSERT INTO `tags` VALUES ('Local/s1500/Pason/pason_EDR_iomap/PASON_BIT_DEPTH_ft', 'Local/s1500/Pason/pason_EDR_iomap/PASON_BIT_DEPTH_ft', 'Bit Depth', '{}', 'ft', NULL);
INSERT INTO `tags` VALUES ('Local/s1500/Pason/pason_EDR_iomap/PASON_STANDPIPE_PRESSURE_psi', 'Local/s1500/Pason/pason_EDR_iomap/PASON_STANDPIPE_PRESSURE_psi', 'Standpipe Pressure', '{}', 'psi', NULL);
INSERT INTO `tags` VALUES ('Local/s1500/Pason/pason_EDR_iomap/PASON_PUMP1_spm', 'Local/s1500/Pason/pason_EDR_iomap/PASON_PUMP1_spm', 'MP1 SPM', '{}', 'spm', NULL);
INSERT INTO `tags` VALUES ('Local/s1500/Pason/pason_EDR_iomap/PASON_PUMP2_spm', 'Local/s1500/Pason/pason_EDR_iomap/PASON_PUMP2_spm', 'MP2 SPM', '{}', 'spm', NULL);
INSERT INTO `tags` VALUES ('Local/s1500/Pason/pason_EDR_iomap/PASON_HOOK_LOAD_klbs', 'Local/s1500/Pason/pason_EDR_iomap/PASON_HOOK_LOAD_klbs', 'Hookload', '{}', 'klbs', NULL);
INSERT INTO `tags` VALUES ('Local/s1500/Pason/pason_EDR_iomap/PASON_BLOCK_HEIGHT_ft', 'Local/s1500/Pason/pason_EDR_iomap/PASON_BLOCK_HEIGHT_ft', 'Block Height', '{}', 'ft', NULL);
INSERT INTO `tags` VALUES ('Local/s1500/Pason/pason_EDR_iomap/PASON_ROTARY_TORQUE_kftlbs', 'Local/s1500/Pason/pason_EDR_iomap/PASON_ROTARY_TORQUE_kftlbs', 'Top Drive Torque', '{}', 'kftlbs', NULL);
INSERT INTO `tags` VALUES ('Local/s1500/Pason/pason_EDR_iomap/PASON_WEIGHT_ON_BIT_klbs', 'Local/s1500/Pason/pason_EDR_iomap/PASON_WEIGHT_ON_BIT_klbs', 'Weight On Bit', '{}', 'klbs', NULL);
INSERT INTO `tags` VALUES ('Local/s1500/Pason/pason_EDR_iomap/PASON_ROP_fph', 'Local/s1500/Pason/pason_EDR_iomap/PASON_ROP_fph', 'ROP', '{}', 'fph', NULL);
INSERT INTO `tags` VALUES ('Local/s1500/Pason/pason_EDR_iomap/PASON_ROTARY_rpm', 'Local/s1500/Pason/pason_EDR_iomap/PASON_ROTARY_rpm', 'Top Drive RPM', '{}', 'RPM', NULL);


INSERT INTO `tags` VALUES ('Local/adr_pilot/Pason/pason_EDR_iomap/HOLE_DEPTH_m', 'Local/adr_pilot/Pason/pason_EDR_iomap/HOLE_DEPTH_m', 'Hole Depth', '{}', 'm', NULL);
INSERT INTO `tags` VALUES ('Local/adr_pilot/Pason/pason_EDR_iomap/BIT_DEPTH_m', 'Local/adr_pilot/Pason/pason_EDR_iomap/BIT_DEPTH_m', 'Bit Depth', '{}', 'm', NULL);
INSERT INTO `tags` VALUES ('Local/adr_pilot/Pason/pason_EDR_iomap/PRESSURE_kPa', 'Local/adr_pilot/Pason/pason_EDR_iomap/PRESSURE_kPa', 'Standpipe Pressure', '{}', 'kPa', NULL);
INSERT INTO `tags` VALUES ('Local/adr_pilot/Pason/pason_EDR_iomap/STROKES_1_SPM', 'Local/adr_pilot/Pason/pason_EDR_iomap/STROKES_1_SPM', 'MP1 SPM', '{}', 'spm', NULL);
INSERT INTO `tags` VALUES ('Local/adr_pilot/Pason/pason_EDR_iomap/STROKES_2_SPM', 'Local/adr_pilot/Pason/pason_EDR_iomap/STROKES_2_SPM', 'MP2 SPM', '{}', 'spm', NULL);
INSERT INTO `tags` VALUES ('Local/adr_pilot/Pason/pason_EDR_iomap/HOOK_LOAD_daN', 'Local/adr_pilot/Pason/pason_EDR_iomap/HOOK_LOAD_daN', 'Hookload', '{}', 'DaN', NULL);
INSERT INTO `tags` VALUES ('Local/adr_pilot/Pason/pason_EDR_iomap/BLOCK_HT_m', 'Local/adr_pilot/Pason/pason_EDR_iomap/BLOCK_HT_m', 'Block Height', '{}', 'm', NULL);
INSERT INTO `tags` VALUES ('Local/adr_pilot/Pason/pason_EDR_iomap/torque_Nm', 'Local/adr_pilot/Pason/pason_EDR_iomap/torque_Nm', 'Top Drive Torque', '{}', 'Nm', NULL);
INSERT INTO `tags` VALUES ('Local/adr_pilot/Pason/pason_EDR_iomap/WT_ON_BIT_daN', 'Local/adr_pilot/Pason/pason_EDR_iomap/WT_ON_BIT_daN', 'Weight On Bit', '{}', 'DaN', NULL);
INSERT INTO `tags` VALUES ('Local/adr_pilot/Pason/pason_EDR_iomap/ROP_mps', 'Local/adr_pilot/Pason/pason_EDR_iomap/ROP_mps', 'ROP', '{}', 'mps', NULL);
INSERT INTO `tags` VALUES ('Local/adr_pilot/Pason/pason_EDR_iomap/ROTARY_RPM', 'Local/adr_pilot/Pason/pason_EDR_iomap/ROTARY_RPM', 'Top Drive RPM', '{}', 'RPM', NULL);


INSERT INTO `tags` VALUES ('Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_HOLE_DEPTH_ft', 'Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_HOLE_DEPTH_ft', 'Hole Depth', '{}', 'ft', NULL);
INSERT INTO `tags` VALUES ('Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_BIT_POSITION_ft', 'Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_BIT_POSITION_ft', 'Bit Depth', '{}', 'ft', NULL);
INSERT INTO `tags` VALUES ('Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_Pump_Pressure_PSI', 'Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_Pump_Pressure_PSI', 'Standpipe Pressure', '{}', 'kPa', NULL);
INSERT INTO `tags` VALUES ('Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_PUMP_SPM_1', 'Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_PUMP_SPM_1', 'MP1 SPM', '{}', 'spm', NULL);
INSERT INTO `tags` VALUES ('Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_PUMP_SPM_2', 'Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_PUMP_SPM_2', 'MP2 SPM', '{}', 'spm', NULL);
INSERT INTO `tags` VALUES ('Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_HOOK_LOAD_klbs', 'Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_HOOK_LOAD_klbs', 'Hookload', '{}', 'klbs', NULL);
INSERT INTO `tags` VALUES ('Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_BLOCK_HEIGHT_ft', 'Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_BLOCK_HEIGHT_ft', 'Block Height', '{}', 'm', NULL);
INSERT INTO `tags` VALUES ('Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_TOP_DRIVE_TORQUE_k ftlb', 'Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_TOP_DRIVE_TORQUE_k ftlb', 'Top Drive Torque', '{}', 'kftlb', NULL);
INSERT INTO `tags` VALUES ('Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_BIT_WEIGHT_klbs', 'Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_BIT_WEIGHT_klbs', 'Weight On Bit', '{}', 'klbs', NULL);
INSERT INTO `tags` VALUES ('Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_ROP_FAST_fph', 'Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_ROP_FAST_fph', 'ROP', '{}', 'fph', NULL);
INSERT INTO `tags` VALUES ('Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_TOP_DRIVE_RPM', 'Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_TOP_DRIVE_RPM', 'Top Drive RPM', '{}', 'RPM', NULL);

'Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_HOLE_DEPTH_ft', 'Hole Depth', 'ft'
'Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_BIT_POSITION_ft', 'Bit Depth', 'ft'
'Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_Pump_Pressure_PSI', 'Standpipe Pressure', 'kPa'
'Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_PUMP_SPM_1', 'MP1 SPM', 'spm'
'Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_PUMP_SPM_2', 'MP2 SPM', 'spm'
'Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_HOOK_LOAD_klbs', 'Hookload', 'klbs'
'Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_BLOCK_HEIGHT_ft', 'Block Height', 'm'
'Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_TOP_DRIVE_TORQUE_k ftlb', 'Top Drive Torque', 'kftlb'
'Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_BIT_WEIGHT_klbs', 'Weight On Bit', 'klbs'
'Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_ROP_FAST_fph', 'ROP', 'fph'
'Local/adr_pilot/NOV/NOV/Ensign_NOV_OPC_Map/MDT_TOP_DRIVE_RPM', 'Top Drive RPM', 'RPM'



INSERT INTO `tags` (`tag_path`, `cimplicity_tag_path`, `display_name`, `definition`, `units`, `folder`) VALUES
	('adr_pilot/autodriller/ad_iomap/hole_depth_m', 'ADR_PILOT/AUTODRILLER/AD_IOMAP/HOLE_DEPTH_M/VALUE', 'Hole Depth', '{}', 'm', NULL),
	('adr_pilot/autodriller/ad_iomap/bit_depth_m', 'ADR_PILOT/AUTODRILLER/AD_IOMAP/BIT_DEPTH_M/VALUE', 'Bit Depth', '{}', 'm', NULL),
	('adr_pilot/autodriller/ad_iomap/standpipepressunfiltpv_kpa', 'adr_pilot/autodriller/ad_iomap/standpipepressunfiltpv_kpa/value', 'Standpipe Pressure', '{}', 'kPa', NULL),
	('ensign_ac_rig/console/aq/mp1_spm', 'ENSIGN_AC_RIG/CONSOLE/AQ/MP1_SPM/VALUE', 'MP1 SPM', '{}', 'spm', NULL),
	('ensign_ac_rig/console/aq/mp2_spm', 'ENSIGN_AC_RIG/CONSOLE/AQ/MP2_SPM/VALUE', 'MP2 SPM', '{}', 'spm', NULL),
	('ensign_ac_rig/dw/rig_hookload', 'ENSIGN_AC_RIG/DW/RIG_HOOKLOAD/VALUE', 'Hookload', '{}', 'DaN', NULL),
	('ensign_ac_rig/dw/block_height', 'ENSIGN_AC_RIG/DW/BLOCK_HEIGHT/VALUE', 'Block Height', '{}', 'feet', NULL),
	('adr_pilot/td_main/istatus/quilltorquepv_knm', 'adr_pilot/td_main/istatus/quilltorquepv_knm/value', 'Top Drive Torque', '{}', 'kNm', NULL),
	('ensign_ac_rig/dw/autodriller_wob_act', 'ensign_ac_rig/dw/autodriller_wob_act/value', 'Weight On Bit', '{}', 'DaN', NULL),
	('ensign_ac_rig/dw/autodriller_rop_act', 'ensign_ac_rig/dw/autodriller_rop_act/value', 'ROP', '{}', 'm/hr', NULL),
	('adr_pilot/td_main/istatus/quillspeedpv_rpm', 'ADR_PILOT/TD_MAIN/ISTATUS/QUILLSPEEDPV_RPM/VALUE', 'Top Drive RPM', '{}', 'RPM', NULL),

	

	('ensign_ac_rig/abb_dw/igbt_temp', 'ensign_ac_rig/abb_dw/igbt_temp/value', 'IGBT Temperature', '{}', 'C', NULL),
	('ensign_ac_rig/abb_dw/output_voltage', 'ensign_ac_rig/abb_dw/output_voltage/value', 'DW Output Voltage', '{}', 'V', NULL),
	('ensign_ac_rig/abb_dw/dc_bus_voltage', 'ensign_ac_rig/abb_dw/dc_bus_voltage/value', 'DW DC Bus Voltage', '{}', 'V', NULL),
	('ensign_ac_rig/abb_dw/speed', 'ensign_ac_rig/abb_dw/speed/value', 'DW Speed', '{}', 'ft/min', NULL),
	('ensign_ac_rig/abb_dw/current', 'ensign_ac_rig/abb_dw/current/value', 'DW Current', '{}', 'amp', NULL),
	('ensign_ac_rig/abb_dw/frequency', 'ensign_ac_rig/abb_dw/frequency/value', 'DW Frequency', '{}', 'hertz', NULL),
	('ensign_ac_rig/abb_dw/torque', 'ensign_ac_rig/abb_dw/torque', 'Drawworks Torque', '{}', 'kft-lbf', NULL);
