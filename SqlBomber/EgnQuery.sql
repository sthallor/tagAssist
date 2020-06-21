select Division, nice_name as RigNumber, control_system as ControlSystem, EGN as Server
from sqlth_drv 
where division <> 'ENT' and active = 'Y' and rig_status = 'Active' and EGN is not null