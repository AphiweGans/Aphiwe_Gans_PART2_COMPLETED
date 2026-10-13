-- IMPORTANT: This is the Part 1 reference/seed script, not the EF Core migration.
-- The PasswordHash seed values below are placeholders and cannot be used to log in.
-- For the Part 2 API database, apply the committed EF Core migration and create
-- demo accounts through POST /api/auth/register so BCrypt hashes are generated.

-- Database name 
USE Raceday;
-- 1. ORGANISERS

CREATE TABLE Organisers (
    OrganiserID INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Phone NVARCHAR(20) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);

-- 2. PARTICIPANTS
CREATE TABLE Participants (
    ParticipantID INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    DateOfBirth DATE NULL,
    Gender NVARCHAR(20) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);

-- 3. EVENTS  

CREATE TABLE Events (
    EventID INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserID INT NOT NULL,
    EventName NVARCHAR(150) NOT NULL,
    EventDate DATE NOT NULL,
    Location NVARCHAR(150) NOT NULL,
    Description NVARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Events_Organisers FOREIGN KEY (OrganiserID)
        REFERENCES Organisers(OrganiserID)
);


-- 4. CATEGORIES  
CREATE TABLE Categories (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    EventID INT NOT NULL,
    CategoryName NVARCHAR(50) NOT NULL,
    DistanceKm DECIMAL(5,2) NOT NULL,
    MaxParticipants INT NOT NULL DEFAULT 100,
    EntryFee DECIMAL(8,2) NOT NULL DEFAULT 0,
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventID)
        REFERENCES Events(EventID)
);
-- 5. ENROLMENTS  

CREATE TABLE Enrolments (
    EnrolmentID INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID INT NOT NULL,
    CategoryID INT NOT NULL,
    EnrolmentDate DATETIME NOT NULL DEFAULT GETDATE(),
    Status NVARCHAR(20) NOT NULL DEFAULT 'Confirmed',
    CONSTRAINT FK_Enrolments_Participants FOREIGN KEY (ParticipantID)
        REFERENCES Participants(ParticipantID),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryID)
        REFERENCES Categories(CategoryID),
    CONSTRAINT UQ_Enrolment UNIQUE (ParticipantID, CategoryID)
);


-- 6. RESULTS  
CREATE TABLE Results (
    ResultID INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID INT NOT NULL UNIQUE,
    FinishTime TIME NULL,
    Position INT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Finished',
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentID)
        REFERENCES Enrolments(EnrolmentID)
);

-- SEED DATA
-- Minimum: 2 Organisers, 2 Participants, 3 Events,
-- categories for each event, sample enrolments

INSERT INTO Organisers (FullName, Email, PasswordHash, Phone) VALUES
('Aphiwe Gans', 'aphiwe@raceday.co.za', 'hashed_pw_1', '0821234567'),
('Chuma Mafu', 'chuma@raceday.co.za', 'hashed_pw_2', '0837654321');

INSERT INTO Participants (FullName, Email, PasswordHash, DateOfBirth, Gender) VALUES
('Kwakhe Khumalo', 'kwakhe@example.com', 'hashed_pw_3', '1995-03-12', 'Female'),
('Abongile Manqana', 'abongile@example.com', 'hashed_pw_4', '1990-07-22', 'Male');

INSERT INTO Events (OrganiserID, EventName, EventDate, Location, Description) VALUES
(1, 'Joburg City Marathon', '2026-05-10', 'Johannesburg', 'Annual road running event through the city.'),
(1, 'Cape Coastal Cycle Tour', '2026-06-14', 'Cape Town', 'Scenic cycling tour along the coast.'),
(2, 'Durban Beachfront Park Run', '2026-04-05', 'Durban', 'Community 5km and 10km walk/run event.');

INSERT INTO Categories (EventID, CategoryName, DistanceKm, MaxParticipants, EntryFee) VALUES
(1, '10km', 10, 500, 150.00),
(1, '21km', 21, 300, 250.00),
(2, '50km Cycle', 50, 200, 300.00),
(3, '5km', 5, 400, 50.00);

INSERT INTO Enrolments (ParticipantID, CategoryID, Status) VALUES
(1, 1, 'Confirmed'),
(2, 2, 'Confirmed'),
(1, 4, 'Confirmed');

INSERT INTO Results (EnrolmentID, FinishTime, Position, Status) VALUES
(1, '00:52:30', 3, 'Finished'),
(2, '01:58:10', 12, 'Finished');
