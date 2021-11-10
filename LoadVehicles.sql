insert into BusinessUnit(Code, Name) values ('BU1', 'Here'), ('BU2', 'There')

insert into Equipment(Code, Name) values ('EQ1', 'This'), ('EQ2', 'That')


declare @nl table(Code nvarchar(20), Name nvarchar(20))
insert into @nl(Code, Name) values ('Emergency', 'Emergency'), ('Alert', 'Alert'), ('Warning', 'Warning'), ('Info', 'Info')
insert into NotificationLevel(Code, Name)
select Code, Name from @nl nl 
where not exists (select 1 from NotificationLevel where Code = nl.Code)

declare @nr table(Code nvarchar(20), Name nvarchar(20), DefLevel nvarchar(20))
insert into @nr(Code, Name, DefLevel) values ('A1', 'Vehicle Status', 'Alert'), ('A3', 'Invalid Registration', 'Alert'), ('N1', 'No Fleet Number', 'Info')
insert into NotificationRuke(Code, Name, DefaultNotificationLevelId)
select nr.Code, nr.Name, nl.NotificationLevelId  from @nr nr inner join NotificationLevel nl on nr.DefLevel = nl.Code
where not exists (select 1 from NotificationRule where Code = nr.Code)


declare @v table(VehicleNo nvarchar(20), BusUnit nvarchar(10))
insert into @v(VehicleNo, BusUnit) values ('123ABC', 'BU1'), ('YXZ', 'BU1'), ('UYH', 'BU2')
insert into Vehicle(VehicleNo, BusinessUnitId)
select v.VehicleNo, b.BusinessUnitId from @v v inner join BusinessUnit b on v.BusUnit = b.Code where not exists (select 1 from Vehicle where VehicleNo = v.VehicleNo)

declare @vl table(VehicleNo nvarchar(20), LastUpdated datetimeoffset, Lat float, Lng float, Speed smallint, Dir smallint)
insert into @vl(VehicleNo, LastUpdated, Lat, Lng, Speed, Dir) values
('123ABC', sysdatetimeoffset(), -37.84326999134972, 144.97838640368965, 55, 150),
('YXZ', sysdatetimeoffset(), -37.822662054015176, 144.87229967486553, 35, 45)
insert into VehicleLocation(VehicleId, LastUpdated, Latitude, Longitude, Speed, Direction)
select  v.VehicleId, vl.LastUpdated, vl.Lat, vl.Lng, vl.Speed, vl.Dir
from @vl vl inner join Vehicle v on v.VehicleNo = vl.VehicleNo 
where not exists (select 1 from VehicleLocation where VehicleId = v.VehicleId)


select
	v.VehicleId,
	v.VehicleNo,
	bu.Code as BusinessUnitCode,
	bu.Name as BusinessUnitName,
	l.LastUpdated,
	l.Latitude,
	l.Longitude,
	l.Speed,
	l.Direction
from
	Vehicle v
	inner join BusinessUnit bu on bu.BusinessUnitId = v.BusinessUnitId
	left join VehicleLocation l on l.VehicleId = v.VehicleId