CREATE TABLE MentalHealthEntries
(
EntryID INT IDENTITY(1,1) PRIMARY KEY,
userID INT,
Date datetime,
Mood INT,
Notes VARCHAR(MAX),
Triggers VARCHAR(Max)
)
CREATE TABLE Users
(
UserId INT IDENTITY(1,1) PRIMARY KEY,
Username nvarchar(50) not null,
PasswordHash nvarchar(max) not null
)

