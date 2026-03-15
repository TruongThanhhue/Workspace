create database WorkspaceDB
use WorkspaceDB
CREATE TABLE Users
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100),
    Email NVARCHAR(100) UNIQUE,
    Password NVARCHAR(200),
    Avatar NVARCHAR(255),
    CreatedAt DATETIME DEFAULT GETDATE()
)
go
CREATE TABLE Projects
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200),
    Description NVARCHAR(MAX),
    ManagerId INT,
    CreatedAt DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (ManagerId) REFERENCES Users(Id)
)
go
CREATE TABLE ProjectMembers
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProjectId INT,
    UserId INT,
    Role NVARCHAR(20),
    JoinedAt DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (ProjectId) REFERENCES Projects(Id),
    FOREIGN KEY (UserId) REFERENCES Users(Id)
)
go
CREATE TABLE JoinRequests
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProjectId INT,
    UserId INT,
    Status NVARCHAR(20),
    CreatedAt DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (ProjectId) REFERENCES Projects(Id),
    FOREIGN KEY (UserId) REFERENCES Users(Id)
)
go
CREATE TABLE InviteLinks
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProjectId INT,
    InviteCode NVARCHAR(100),
    CreatedBy INT,
    ExpiredAt DATETIME,

    FOREIGN KEY (ProjectId) REFERENCES Projects(Id),
    FOREIGN KEY (CreatedBy) REFERENCES Users(Id)
)
go
CREATE TABLE Tasks
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProjectId INT,
    Title NVARCHAR(200),
    Description NVARCHAR(MAX),
    AssignedTo INT,
    Status NVARCHAR(20),
    Priority NVARCHAR(20),
    StartDate DATETIME,
    EndDate DATETIME,
    CreatedAt DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (ProjectId) REFERENCES Projects(Id),
    FOREIGN KEY (AssignedTo) REFERENCES Users(Id)
)
CREATE TABLE TaskTimeline
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TaskId INT,
    StartTime DATETIME,
    EndTime DATETIME,

    FOREIGN KEY (TaskId) REFERENCES Tasks(Id)
)
go
CREATE TABLE Comments
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TaskId INT,
    UserId INT,
    Content NVARCHAR(MAX),
    CreatedAt DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (TaskId) REFERENCES Tasks(Id),
    FOREIGN KEY (UserId) REFERENCES Users(Id)
)
go
CREATE TABLE Attachments
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TaskId INT,
    UserId INT,
    FilePath NVARCHAR(255),
    UploadedAt DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (TaskId) REFERENCES Tasks(Id),
    FOREIGN KEY (UserId) REFERENCES Users(Id)
)
go
CREATE TABLE Evaluations
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TaskId INT,
    UserId INT,
    Score INT,
    Feedback NVARCHAR(MAX),
    EvaluatedBy INT,
    CreatedAt DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (TaskId) REFERENCES Tasks(Id),
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (EvaluatedBy) REFERENCES Users(Id)
)
go
CREATE TABLE Notifications
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT,
    Content NVARCHAR(MAX),
    IsRead BIT DEFAULT 0,
    CreatedAt DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (UserId) REFERENCES Users(Id)
)
go
CREATE TABLE ActivityLogs
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT,
    Action NVARCHAR(200),
    CreatedAt DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (UserId) REFERENCES Users(Id)
)
ALTER TABLE Projects ADD ImageUrl NVARCHAR(255) NULL;
SELECT * FROM JoinRequests;
CREATE TABLE Messages (
    Id INT PRIMARY KEY IDENTITY,
    ProjectId INT FOREIGN KEY REFERENCES Projects(Id) ON DELETE CASCADE,
    SenderId INT FOREIGN KEY REFERENCES Users(Id),
    ReceiverId INT NULL FOREIGN KEY REFERENCES Users(Id),
    Content NVARCHAR(1000) NOT NULL,
    SentAt DATETIME DEFAULT GETDATE()
);
CREATE TABLE Submissions (
    Id INT PRIMARY KEY IDENTITY,
    TaskId INT FOREIGN KEY REFERENCES Tasks(Id) ON DELETE CASCADE,
    UserId INT FOREIGN KEY REFERENCES Users(Id),
    FileUrl NVARCHAR(500) NULL,
    LinkUrl NVARCHAR(500) NULL,
    Note NVARCHAR(1000) NULL,
    Status NVARCHAR(20) DEFAULT 'Pending',
    SubmittedAt DATETIME DEFAULT GETDATE()
);
ALTER TABLE Users ADD Role NVARCHAR(20) DEFAULT 'Member';
ALTER TABLE Projects ADD StartDate DATETIME DEFAULT GETDATE();
ALTER TABLE Projects ADD EndDate DATETIME DEFAULT GETDATE();
ALTER TABLE ProjectMembers ALTER COLUMN Role NVARCHAR(20) NOT NULL;
ALTER TABLE Notifications ADD Url NVARCHAR(500) NULL;
UPDATE n
SET n.Url = '/Task/Details/' + CAST(t.Id AS NVARCHAR)
FROM Notifications n
JOIN Tasks t ON n.Content LIKE '%' + t.Title + '%'
WHERE n.Url IS NULL
UPDATE Notifications SET Url = '/Task/Details/1' WHERE Id = 1
SELECT * FROM Notifications
SELECT Id, Title FROM Tasks
UPDATE n
SET n.Url = '/Task/Details/' + CAST(t.Id AS NVARCHAR)
FROM Notifications n
JOIN Tasks t ON n.Content LIKE N'%' + t.Title + N'%'
CREATE TABLE ProjectInvites (
    Id INT PRIMARY KEY IDENTITY,
    ProjectId INT NOT NULL REFERENCES Projects(Id) ON DELETE CASCADE,
    InvitedUserId INT NOT NULL REFERENCES Users(Id),
    InvitedByUserId INT NOT NULL REFERENCES Users(Id),
    Status NVARCHAR(20) DEFAULT 'Pending',
    CreatedAt DATETIME DEFAULT GETDATE()
);