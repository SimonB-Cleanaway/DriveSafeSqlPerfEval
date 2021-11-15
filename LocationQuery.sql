-- select count(*) from VehicleTrace

declare @latitude float = -37.8396
declare @longitude float = 144.9772
declare @distance int = 10000; -- 500 km
declare @from DateTimeOffset = DATEADD(hour, -1, SYSDATETIMEOFFSET())
declare @to DateTimeOffset = DATEADD(hour, 1, SYSDATETIMEOFFSET())

select v.VehicleId, v.VehicleNo, vt.Timestamp, vt.Location.Lat as Latitude, vt.Location.Long as Longitude, vt.Speed, vt.Direction 
from VehicleTrace vt inner join Vehicle v on v.VehicleId = vt.VehicleId 
where geography::Point(@latitude, @longitude, 4326).STDistance(vt.Location) <= @distance and vt.Timestamp >= @from and vt.Timestamp <= @to

-- declare @point geography = geography::Point(@latitude, @longitude, 4326);
--select v.VehicleId, v.VehicleNo, vt.Timestamp, vt.Location.Lat as Latitude, vt.Location.Long as Longitude, vt.Speed, vt.Direction 
--from VehicleTrace vt inner join Vehicle v on v.VehicleId = vt.VehicleId 
--where @point.STDistance(vt.Location) <= @distance and vt.Timestamp >= @from and vt.Timestamp <= @to;
