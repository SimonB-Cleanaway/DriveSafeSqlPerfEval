if exists(select * from sys.tables where name = 'VehicleNotification') 
	drop table VehicleNotification

if exists(select * from sys.tables where name = 'VehicleLocation') 
	drop table VehicleLocation

if exists(select * from sys.tables where name = 'VehicleEquipment') 
	drop table VehicleEquipment

if exists(select * from sys.tables where name = 'Vehicle') 
	drop table Vehicle

if exists(select * from sys.tables where name = 'BusinessUnit') 
	drop table BusinessUnit

if exists(select * from sys.tables where name = 'Equipment') 
	drop table Equipment

if exists(select * from sys.tables where name = 'NotificationRule') 
	drop table NotificationRule

if exists(select * from sys.tables where name = 'NotificationLevel') 
	drop table NotificationLevel

go

create table NotificationLevel
(
	NotificationLevelId int not null identity(1,1) primary key,
	Code nvarchar(20) not null unique,
	Name nvarchar(80) not null unique,
)

create table NotificationRule
(
	NotificationRuleId int not null identity(1,1) primary key,
	Code nvarchar(20) not null unique,
	Name nvarchar(80) not null unique,
	DefaultNotificationLevelId int not null foreign key references NotificationLevel(NotificationLevelId)
)

create table BusinessUnit
(
	BusinessUnitId int not null identity(1,1) primary key,
	Code nvarchar(20) not null unique,
	Name nvarchar(80) not null unique
)

create table Equipment
(
	EquipmentId int not null identity(1,1) primary key,
	Code nvarchar(20) not null unique,
	Name nvarchar(80) not null unique
)

create table Vehicle
(
	VehicleId int not null identity(1,1) primary key,
	VehicleNo nvarchar(20) unique,
	BusinessUnitId int not null foreign key references BusinessUnit(BusinessUnitId)
)

create table VehicleEquipment
(
	VehicleId int not null foreign key references Vehicle(VehicleId),
	EquipmentId int not null foreign key references Equipment(EquipmentId),

	constraint VehicleEquipment_pk primary key (VehicleId, EquipmentId)
);

create table VehicleLocation
(
	VehicleId int not null foreign key references Vehicle(VehicleId) primary key,
	LastUpdated datetimeoffset(2) not null,
	Latitude float null,
	Longitude float null,
	Direction smallint null,
	Speed smallint null
);

create table VehicleNotification
(
	VehicleNotificationId int not null identity(1,1) primary key,
	VehicleId int not null foreign key references Vehicle(VehicleId),
	NotificationRuleId int not null foreign key references NotificationRule(NotificationRuleId),
	NotificationLevelId int not null foreign key references NotificationLevel(NotificationLevelId),
	Message nvarchar(120) null,
	CreateDate datetimeoffset not null,
	ExpireDate datetimeoffset null,
	Active bit not null default 1
)

