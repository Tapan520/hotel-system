CREATE DATABASE  IF NOT EXISTS `hotelchannelmanager` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;
USE `hotelchannelmanager`;
-- MySQL dump 10.13  Distrib 8.0.45, for Win64 (x86_64)
--
-- Host: localhost    Database: hotelchannelmanager
-- ------------------------------------------------------
-- Server version	8.0.45

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `auditlogs`
--

DROP TABLE IF EXISTS `auditlogs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `auditlogs` (
  `LogId` int NOT NULL AUTO_INCREMENT,
  `UserId` int DEFAULT NULL,
  `Action` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Module` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `RecordId` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `OldValues` json DEFAULT NULL,
  `NewValues` json DEFAULT NULL,
  `IPAddress` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `UserAgent` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Notes` text COLLATE utf8mb4_unicode_ci,
  `CreatedAt` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`LogId`),
  KEY `idx_action` (`Action`),
  KEY `idx_created` (`CreatedAt`)
) ENGINE=InnoDB AUTO_INCREMENT=29 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `auditlogs`
--

LOCK TABLES `auditlogs` WRITE;
/*!40000 ALTER TABLE `auditlogs` DISABLE KEYS */;
INSERT INTO `auditlogs` VALUES (1,1,'LOGIN','Auth',NULL,NULL,NULL,NULL,NULL,'Login from ::1','2026-03-03 19:03:01'),(2,1,'LOGIN','Auth',NULL,NULL,NULL,NULL,NULL,'Login from ::1','2026-03-03 19:03:43'),(3,1,'LOGIN','Auth',NULL,NULL,NULL,NULL,NULL,'Login from ::1','2026-03-04 15:29:23'),(4,1,'LOGIN','Auth',NULL,NULL,NULL,NULL,NULL,'Login from ::1','2026-03-04 15:50:55'),(5,1,'LOGIN','Auth',NULL,NULL,NULL,NULL,NULL,'Login from ::1','2026-03-04 22:37:16'),(6,1,'LOGIN','Auth',NULL,NULL,NULL,NULL,NULL,'Login from ::1','2026-03-04 22:37:44'),(7,1,'LOGIN','Auth',NULL,NULL,NULL,NULL,NULL,'Login from ::1','2026-03-04 22:59:47'),(8,1,'LOGIN','Auth',NULL,NULL,NULL,NULL,NULL,'Login from ::1','2026-03-04 23:02:38'),(9,1,'LOGIN','Auth',NULL,NULL,NULL,NULL,NULL,'Login from ::1','2026-03-05 14:35:43'),(10,1,'LOGIN','Auth',NULL,NULL,NULL,NULL,NULL,'Login from ::1','2026-03-09 16:06:57'),(11,2,'LOGIN','Auth',NULL,NULL,NULL,NULL,NULL,'Login from ::1','2026-03-09 16:14:41'),(12,3,'LOGIN','Auth',NULL,NULL,NULL,NULL,NULL,'Login from ::1','2026-03-09 16:16:02'),(13,1,'LOGIN','Auth',NULL,NULL,NULL,NULL,NULL,'Login from ::1','2026-03-09 16:23:21'),(14,3,'LOGIN','Auth',NULL,NULL,NULL,NULL,NULL,'Login from ::1','2026-03-09 16:42:52'),(15,2,'LOGIN','Auth',NULL,NULL,NULL,NULL,NULL,'Login from ::1','2026-03-09 16:43:25'),(16,3,'LOGIN','Auth',NULL,NULL,NULL,NULL,NULL,'Login from ::1','2026-03-09 16:44:16'),(17,1,'LOGIN','Auth',NULL,NULL,NULL,NULL,NULL,'Login from ::1','2026-03-09 18:18:06'),(18,1,'LOGIN','Auth',NULL,NULL,NULL,NULL,NULL,'Login from ::1','2026-03-09 18:29:32'),(19,1,'LOGIN','Auth',NULL,NULL,NULL,NULL,NULL,'Login from ::1','2026-03-09 21:57:00'),(20,2,'LOGIN','Auth',NULL,NULL,NULL,NULL,NULL,'Login from ::1','2026-03-09 22:10:34'),(21,3,'LOGIN','Auth',NULL,NULL,NULL,NULL,NULL,'Login from ::1','2026-03-09 22:36:23'),(22,1,'LOGIN','Auth',NULL,NULL,NULL,NULL,NULL,'Login from ::1','2026-03-10 13:03:21'),(23,1,'CHECKIN','Bookings','1',NULL,NULL,NULL,NULL,NULL,'2026-03-10 13:08:23'),(24,1,'LOGIN','Auth',NULL,NULL,NULL,NULL,NULL,'Login from ::1','2026-03-10 15:32:16'),(25,1,'LOGIN','Auth',NULL,NULL,NULL,NULL,NULL,'Login from ::1','2026-03-10 16:35:02'),(26,1,'CREATE_BOOKING','Bookings','BK2026031011072501',NULL,NULL,NULL,NULL,NULL,'2026-03-10 16:37:26'),(27,1,'CREATE_BOOKING','Bookings','BK2026031011094901',NULL,NULL,NULL,NULL,NULL,'2026-03-10 16:39:49'),(28,1,'LOGIN','Auth',NULL,NULL,NULL,NULL,NULL,'Login from ::1','2026-03-18 16:16:27');
/*!40000 ALTER TABLE `auditlogs` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `billentries`
--

DROP TABLE IF EXISTS `billentries`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `billentries` (
  `BillEntryId` int NOT NULL AUTO_INCREMENT,
  `BookingId` int NOT NULL,
  `EntryType` enum('RoomCharge','RoomService','Restaurant','Laundry','MiniBar','ServiceRequest','AddOn','Discount','TaxAdjustment','ManualCharge','Refund','Payment') COLLATE utf8mb4_unicode_ci NOT NULL,
  `Description` varchar(500) COLLATE utf8mb4_unicode_ci NOT NULL,
  `ReferenceId` int DEFAULT NULL,
  `ReferenceType` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Amount` decimal(10,2) NOT NULL,
  `TaxAmount` decimal(10,2) DEFAULT '0.00',
  `GrandAmount` decimal(10,2) NOT NULL,
  `PostedAt` datetime DEFAULT CURRENT_TIMESTAMP,
  `PostedBy` int DEFAULT NULL,
  `IsVoided` tinyint(1) DEFAULT '0',
  `VoidedAt` datetime DEFAULT NULL,
  `VoidedBy` int DEFAULT NULL,
  `VoidReason` text COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`BillEntryId`),
  KEY `idx_booking` (`BookingId`),
  KEY `idx_type` (`EntryType`),
  KEY `idx_posted` (`PostedAt`),
  CONSTRAINT `billentries_ibfk_1` FOREIGN KEY (`BookingId`) REFERENCES `bookings` (`BookingId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `billentries`
--

LOCK TABLES `billentries` WRITE;
/*!40000 ALTER TABLE `billentries` DISABLE KEYS */;
/*!40000 ALTER TABLE `billentries` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `bookings`
--

DROP TABLE IF EXISTS `bookings`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `bookings` (
  `BookingId` int NOT NULL AUTO_INCREMENT,
  `BookingReference` varchar(30) COLLATE utf8mb4_unicode_ci NOT NULL,
  `HotelId` int NOT NULL,
  `RoomTypeId` int NOT NULL,
  `RoomId` int DEFAULT NULL,
  `CustomerId` int NOT NULL,
  `PartnerId` int DEFAULT NULL,
  `CheckInDate` date NOT NULL,
  `CheckOutDate` date NOT NULL,
  `TotalNights` int GENERATED ALWAYS AS ((to_days(`CheckOutDate`) - to_days(`CheckInDate`))) STORED,
  `AdultsCount` int DEFAULT '1',
  `ChildrenCount` int DEFAULT '0',
  `RoomRate` decimal(10,2) NOT NULL,
  `SubTotal` decimal(10,2) NOT NULL,
  `TaxAmount` decimal(10,2) DEFAULT '0.00',
  `DiscountAmount` decimal(10,2) DEFAULT '0.00',
  `GrandTotal` decimal(10,2) NOT NULL,
  `CommissionAmount` decimal(10,2) DEFAULT '0.00',
  `NetToHotel` decimal(10,2) DEFAULT NULL,
  `AmountPaid` decimal(10,2) DEFAULT '0.00',
  `BalanceDue` decimal(10,2) GENERATED ALWAYS AS ((`GrandTotal` - `AmountPaid`)) STORED,
  `PaymentMode` enum('PayAtHotel','OnlinePaid') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'PayAtHotel',
  `BookingStatus` enum('Pending','Confirmed','CheckedIn','CheckedOut','Cancelled','NoShow','WaitList') COLLATE utf8mb4_unicode_ci DEFAULT 'Confirmed',
  `BookingSource` enum('HotelDesk','Website','ChannelPartner','Phone','Email','WalkIn') COLLATE utf8mb4_unicode_ci DEFAULT 'HotelDesk',
  `SpecialRequests` text COLLATE utf8mb4_unicode_ci,
  `InternalNotes` text COLLATE utf8mb4_unicode_ci,
  `CancellationReason` text COLLATE utf8mb4_unicode_ci,
  `CancellationCharge` decimal(10,2) DEFAULT '0.00',
  `CancelledAt` datetime DEFAULT NULL,
  `CancelledBy` int DEFAULT NULL,
  `ConfirmedAt` datetime DEFAULT CURRENT_TIMESTAMP,
  `CheckedInAt` datetime DEFAULT NULL,
  `CheckedOutAt` datetime DEFAULT NULL,
  `CreatedAt` datetime DEFAULT CURRENT_TIMESTAMP,
  `UpdatedAt` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`BookingId`),
  UNIQUE KEY `BookingReference` (`BookingReference`),
  KEY `HotelId` (`HotelId`),
  KEY `RoomTypeId` (`RoomTypeId`),
  KEY `RoomId` (`RoomId`),
  KEY `CustomerId` (`CustomerId`),
  KEY `PartnerId` (`PartnerId`),
  CONSTRAINT `bookings_ibfk_1` FOREIGN KEY (`HotelId`) REFERENCES `hotels` (`HotelId`),
  CONSTRAINT `bookings_ibfk_2` FOREIGN KEY (`RoomTypeId`) REFERENCES `roomtypes` (`RoomTypeId`),
  CONSTRAINT `bookings_ibfk_3` FOREIGN KEY (`RoomId`) REFERENCES `rooms` (`RoomId`),
  CONSTRAINT `bookings_ibfk_4` FOREIGN KEY (`CustomerId`) REFERENCES `customers` (`CustomerId`),
  CONSTRAINT `bookings_ibfk_5` FOREIGN KEY (`PartnerId`) REFERENCES `channelpartners` (`PartnerId`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `bookings`
--

LOCK TABLES `bookings` WRITE;
/*!40000 ALTER TABLE `bookings` DISABLE KEYS */;
INSERT INTO `bookings` (`BookingId`, `BookingReference`, `HotelId`, `RoomTypeId`, `RoomId`, `CustomerId`, `PartnerId`, `CheckInDate`, `CheckOutDate`, `AdultsCount`, `ChildrenCount`, `RoomRate`, `SubTotal`, `TaxAmount`, `DiscountAmount`, `GrandTotal`, `CommissionAmount`, `NetToHotel`, `AmountPaid`, `PaymentMode`, `BookingStatus`, `BookingSource`, `SpecialRequests`, `InternalNotes`, `CancellationReason`, `CancellationCharge`, `CancelledAt`, `CancelledBy`, `ConfirmedAt`, `CheckedInAt`, `CheckedOutAt`, `CreatedAt`, `UpdatedAt`) VALUES (1,'BK20260309864117',1,2,NULL,6,NULL,'2026-03-09','2026-03-10',2,0,7500.00,7500.00,75.00,0.00,7575.00,0.00,7500.00,0.00,'PayAtHotel','CheckedIn','Website','',NULL,NULL,0.00,NULL,NULL,'2026-03-09 18:27:44','2026-03-10 13:08:23',NULL,'2026-03-09 18:27:44','2026-03-10 13:08:23'),(2,'BK2026031010455201',1,1,NULL,11,NULL,'2026-03-10','2026-03-11',2,0,4500.00,4500.00,45.00,0.00,4545.00,0.00,4545.00,0.00,'PayAtHotel','Confirmed','Website','',NULL,NULL,0.00,NULL,NULL,'2026-03-10 16:15:52',NULL,NULL,'2026-03-10 16:15:52','2026-03-10 16:15:52'),(3,'BK2026031011072501',1,1,NULL,12,NULL,'2026-03-11','2026-03-12',2,0,2000.00,2000.00,20.00,0.00,2020.00,0.00,2020.00,0.00,'PayAtHotel','Confirmed','HotelDesk','NA',NULL,NULL,0.00,NULL,NULL,'2026-03-10 16:37:25',NULL,NULL,'2026-03-10 16:37:25','2026-03-10 16:37:25'),(4,'BK2026031011094901',1,1,NULL,12,NULL,'2026-03-11','2026-03-12',2,0,2000.00,2000.00,20.00,0.00,2020.00,0.00,2020.00,0.00,'PayAtHotel','Confirmed','HotelDesk','NA',NULL,NULL,0.00,NULL,NULL,'2026-03-10 16:39:49',NULL,NULL,'2026-03-10 16:39:49','2026-03-10 16:39:49');
/*!40000 ALTER TABLE `bookings` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `channelpartners`
--

DROP TABLE IF EXISTS `channelpartners`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `channelpartners` (
  `PartnerId` int NOT NULL AUTO_INCREMENT,
  `HotelId` int NOT NULL,
  `PartnerName` varchar(150) COLLATE utf8mb4_unicode_ci NOT NULL,
  `PartnerCode` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `PartnerType` enum('OTA','GDS','Direct','Corporate','Wholesale','MetaSearch') COLLATE utf8mb4_unicode_ci DEFAULT 'OTA',
  `Description` text COLLATE utf8mb4_unicode_ci,
  `LogoUrl` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `APIKey` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `APISecret` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `WebhookURL` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `WebhookSecret` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `CommissionPercent` decimal(5,2) DEFAULT '0.00',
  `PaymentMode` enum('PayAtHotel','OnlineCollect') COLLATE utf8mb4_unicode_ci DEFAULT 'OnlineCollect',
  `RemittanceDays` int DEFAULT '30',
  `ContractStartDate` date DEFAULT NULL,
  `ContractEndDate` date DEFAULT NULL,
  `ContactName` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `ContactEmail` varchar(150) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `ContactPhone` varchar(30) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `IsActive` tinyint(1) DEFAULT '1',
  `CreatedAt` datetime DEFAULT CURRENT_TIMESTAMP,
  `UpdatedAt` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`PartnerId`),
  UNIQUE KEY `uq_partner_code` (`HotelId`,`PartnerCode`),
  CONSTRAINT `channelpartners_ibfk_1` FOREIGN KEY (`HotelId`) REFERENCES `hotels` (`HotelId`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `channelpartners`
--

LOCK TABLES `channelpartners` WRITE;
/*!40000 ALTER TABLE `channelpartners` DISABLE KEYS */;
INSERT INTO `channelpartners` VALUES (1,1,'Booking.com','BOOKING_COM','OTA',NULL,NULL,NULL,NULL,NULL,NULL,15.00,'OnlineCollect',30,NULL,NULL,NULL,'partner@booking.com',NULL,1,'2026-03-02 21:43:25','2026-03-02 21:43:25'),(2,1,'Expedia','EXPEDIA','OTA',NULL,NULL,NULL,NULL,NULL,NULL,18.00,'OnlineCollect',30,NULL,NULL,NULL,'partner@expedia.com',NULL,1,'2026-03-02 21:43:25','2026-03-02 21:43:25'),(3,1,'MakeMyTrip','MMT','OTA',NULL,NULL,NULL,NULL,NULL,NULL,12.00,'OnlineCollect',15,NULL,NULL,NULL,'partner@makemytrip.com',NULL,1,'2026-03-02 21:43:25','2026-03-02 21:43:25'),(4,1,'Agoda','AGODA','OTA',NULL,NULL,NULL,NULL,NULL,NULL,14.00,'PayAtHotel',0,NULL,NULL,NULL,'partner@agoda.com',NULL,1,'2026-03-02 21:43:25','2026-03-02 21:43:25'),(5,1,'TripAdvisor','TRIPADVISOR','MetaSearch',NULL,NULL,NULL,NULL,NULL,NULL,10.00,'OnlineCollect',30,NULL,NULL,NULL,'partner@tripadvisor.com',NULL,1,'2026-03-02 21:43:25','2026-03-02 21:43:25');
/*!40000 ALTER TABLE `channelpartners` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `channelratemappings`
--

DROP TABLE IF EXISTS `channelratemappings`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `channelratemappings` (
  `MappingId` int NOT NULL AUTO_INCREMENT,
  `PartnerId` int NOT NULL,
  `RoomTypeId` int NOT NULL,
  `MarkupPercent` decimal(5,2) DEFAULT '0.00',
  `IsActive` tinyint(1) DEFAULT '1',
  `UpdatedAt` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`MappingId`),
  UNIQUE KEY `uq_channel_rate` (`PartnerId`,`RoomTypeId`),
  KEY `RoomTypeId` (`RoomTypeId`),
  CONSTRAINT `channelratemappings_ibfk_1` FOREIGN KEY (`PartnerId`) REFERENCES `channelpartners` (`PartnerId`) ON DELETE CASCADE,
  CONSTRAINT `channelratemappings_ibfk_2` FOREIGN KEY (`RoomTypeId`) REFERENCES `roomtypes` (`RoomTypeId`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=21 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `channelratemappings`
--

LOCK TABLES `channelratemappings` WRITE;
/*!40000 ALTER TABLE `channelratemappings` DISABLE KEYS */;
INSERT INTO `channelratemappings` VALUES (1,1,1,5.00,1,'2026-03-02 21:43:25'),(2,1,2,5.00,1,'2026-03-02 21:43:25'),(3,1,3,8.00,1,'2026-03-02 21:43:25'),(4,1,4,5.00,1,'2026-03-02 21:43:25'),(5,2,1,8.00,1,'2026-03-02 21:43:25'),(6,2,2,8.00,1,'2026-03-02 21:43:25'),(7,2,3,10.00,1,'2026-03-02 21:43:25'),(8,2,4,8.00,1,'2026-03-02 21:43:25'),(9,3,1,3.00,1,'2026-03-02 21:43:25'),(10,3,2,3.00,1,'2026-03-02 21:43:25'),(11,3,3,5.00,1,'2026-03-02 21:43:25'),(12,3,4,3.00,1,'2026-03-02 21:43:25'),(13,4,1,2.00,1,'2026-03-02 21:43:25'),(14,4,2,2.00,1,'2026-03-02 21:43:25'),(15,4,3,4.00,1,'2026-03-02 21:43:25'),(16,4,4,2.00,1,'2026-03-02 21:43:25'),(17,5,1,4.00,1,'2026-03-02 21:43:25'),(18,5,2,4.00,1,'2026-03-02 21:43:25'),(19,5,3,6.00,1,'2026-03-02 21:43:25'),(20,5,4,4.00,1,'2026-03-02 21:43:25');
/*!40000 ALTER TABLE `channelratemappings` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `checkoutinvoices`
--

DROP TABLE IF EXISTS `checkoutinvoices`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `checkoutinvoices` (
  `InvoiceId` int NOT NULL AUTO_INCREMENT,
  `InvoiceNumber` varchar(30) COLLATE utf8mb4_unicode_ci NOT NULL,
  `BookingId` int NOT NULL,
  `HotelId` int NOT NULL,
  `CustomerId` int NOT NULL,
  `RoomCharges` decimal(10,2) DEFAULT '0.00',
  `ServiceCharges` decimal(10,2) DEFAULT '0.00',
  `TaxTotal` decimal(10,2) DEFAULT '0.00',
  `DiscountTotal` decimal(10,2) DEFAULT '0.00',
  `GrandTotal` decimal(10,2) DEFAULT '0.00',
  `AmountPaid` decimal(10,2) DEFAULT '0.00',
  `BalanceDue` decimal(10,2) GENERATED ALWAYS AS ((`GrandTotal` - `AmountPaid`)) STORED,
  `Status` enum('Draft','Issued','Paid','Partial','Disputed') COLLATE utf8mb4_unicode_ci DEFAULT 'Draft',
  `IssuedAt` datetime DEFAULT NULL,
  `PaidAt` datetime DEFAULT NULL,
  `Notes` text COLLATE utf8mb4_unicode_ci,
  `CreatedAt` datetime DEFAULT CURRENT_TIMESTAMP,
  `UpdatedAt` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`InvoiceId`),
  UNIQUE KEY `InvoiceNumber` (`InvoiceNumber`),
  UNIQUE KEY `BookingId` (`BookingId`),
  KEY `HotelId` (`HotelId`),
  KEY `CustomerId` (`CustomerId`),
  CONSTRAINT `checkoutinvoices_ibfk_1` FOREIGN KEY (`BookingId`) REFERENCES `bookings` (`BookingId`),
  CONSTRAINT `checkoutinvoices_ibfk_2` FOREIGN KEY (`HotelId`) REFERENCES `hotels` (`HotelId`),
  CONSTRAINT `checkoutinvoices_ibfk_3` FOREIGN KEY (`CustomerId`) REFERENCES `customers` (`CustomerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `checkoutinvoices`
--

LOCK TABLES `checkoutinvoices` WRITE;
/*!40000 ALTER TABLE `checkoutinvoices` DISABLE KEYS */;
/*!40000 ALTER TABLE `checkoutinvoices` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `customers`
--

DROP TABLE IF EXISTS `customers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `customers` (
  `CustomerId` int NOT NULL AUTO_INCREMENT,
  `FirstName` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `LastName` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Email` varchar(150) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Phone` varchar(30) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `AlternatePhone` varchar(30) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Address` text COLLATE utf8mb4_unicode_ci,
  `City` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `State` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Country` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `ZipCode` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `IDType` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `IDNumber` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `DateOfBirth` date DEFAULT NULL,
  `Nationality` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Gender` enum('Male','Female','Other','PreferNotToSay') COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `VIPStatus` enum('Regular','Silver','Gold','Platinum') COLLATE utf8mb4_unicode_ci DEFAULT 'Regular',
  `TotalStays` int DEFAULT '0',
  `Notes` text COLLATE utf8mb4_unicode_ci,
  `CreatedAt` datetime DEFAULT CURRENT_TIMESTAMP,
  `UpdatedAt` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`CustomerId`),
  UNIQUE KEY `uq_email` (`Email`)
) ENGINE=InnoDB AUTO_INCREMENT=13 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `customers`
--

LOCK TABLES `customers` WRITE;
/*!40000 ALTER TABLE `customers` DISABLE KEYS */;
INSERT INTO `customers` VALUES (1,'Rahul','Sharma','rahul.sharma@gmail.com','+91-9876543210',NULL,NULL,NULL,NULL,NULL,NULL,'Aadhar Card','1234-5678-9012',NULL,'Indian',NULL,'Gold',0,NULL,'2026-03-02 21:43:25','2026-03-02 21:43:25'),(2,'Priya','Patel','priya.patel@gmail.com','+91-9876543211',NULL,NULL,NULL,NULL,NULL,NULL,'Passport','Z1234567',NULL,'Indian',NULL,'Silver',0,NULL,'2026-03-02 21:43:25','2026-03-02 21:43:25'),(3,'John','Smith','john.smith@example.com','+1-555-0100',NULL,NULL,NULL,NULL,NULL,NULL,'Passport','US123456',NULL,'American',NULL,'Regular',0,NULL,'2026-03-02 21:43:25','2026-03-02 21:43:25'),(6,'Tapan','Singh','tapchauhan2001@gmail.com','919810774490',NULL,NULL,NULL,NULL,NULL,NULL,'PAN Card','AYAPS4612H',NULL,'Indian',NULL,'Regular',1,NULL,'2026-03-09 18:27:44','2026-03-09 21:55:44'),(11,'Manav','Singh','Manvenderthakur@gmail.com','9312794200',NULL,NULL,NULL,NULL,NULL,NULL,'PAN Card','AYAPS4653H',NULL,'Indian',NULL,'Regular',1,NULL,'2026-03-10 16:15:52','2026-03-10 16:15:52'),(12,'Disha','Singhal','disha.singhal1@gmail.com','9717482508',NULL,NULL,NULL,NULL,NULL,NULL,'PAN Card','adadasdas',NULL,'Indian',NULL,'Regular',2,NULL,'2026-03-10 16:37:25','2026-03-10 16:39:49');
/*!40000 ALTER TABLE `customers` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `defaultroomrates`
--

DROP TABLE IF EXISTS `defaultroomrates`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `defaultroomrates` (
  `DefaultRateId` int NOT NULL AUTO_INCREMENT,
  `RoomTypeId` int NOT NULL,
  `WeekdayRate` decimal(10,2) NOT NULL,
  `WeekendRate` decimal(10,2) NOT NULL,
  `EffectiveFrom` date NOT NULL,
  `EffectiveTo` date DEFAULT NULL,
  `CreatedAt` datetime DEFAULT CURRENT_TIMESTAMP,
  `UpdatedAt` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`DefaultRateId`),
  KEY `RoomTypeId` (`RoomTypeId`),
  CONSTRAINT `defaultroomrates_ibfk_1` FOREIGN KEY (`RoomTypeId`) REFERENCES `roomtypes` (`RoomTypeId`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `defaultroomrates`
--

LOCK TABLES `defaultroomrates` WRITE;
/*!40000 ALTER TABLE `defaultroomrates` DISABLE KEYS */;
INSERT INTO `defaultroomrates` VALUES (1,1,4500.00,5500.00,'2026-03-02',NULL,'2026-03-02 21:42:54','2026-03-02 21:42:54'),(2,2,7500.00,9000.00,'2026-03-02',NULL,'2026-03-02 21:42:54','2026-03-02 21:42:54'),(3,3,18000.00,22000.00,'2026-03-02',NULL,'2026-03-02 21:42:54','2026-03-02 21:42:54'),(4,4,5000.00,6000.00,'2026-03-02',NULL,'2026-03-02 21:42:54','2026-03-02 21:42:54');
/*!40000 ALTER TABLE `defaultroomrates` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `hotels`
--

DROP TABLE IF EXISTS `hotels`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `hotels` (
  `HotelId` int NOT NULL AUTO_INCREMENT,
  `HotelName` varchar(200) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Address` text COLLATE utf8mb4_unicode_ci,
  `City` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `State` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Country` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT 'India',
  `ZipCode` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Phone` varchar(30) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Email` varchar(150) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Website` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `StarRating` tinyint DEFAULT '3',
  `CheckInTime` time DEFAULT '14:00:00',
  `CheckOutTime` time DEFAULT '11:00:00',
  `CancellationPolicyHours` int DEFAULT '24',
  `LateCancelChargePercent` decimal(5,2) DEFAULT '50.00',
  `TaxPercent` decimal(5,2) DEFAULT '12.00',
  `CurrencyCode` varchar(10) COLLATE utf8mb4_unicode_ci DEFAULT 'INR',
  `Description` text COLLATE utf8mb4_unicode_ci,
  `IsActive` tinyint(1) DEFAULT '1',
  `CreatedAt` datetime DEFAULT CURRENT_TIMESTAMP,
  `UpdatedAt` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`HotelId`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `hotels`
--

LOCK TABLES `hotels` WRITE;
/*!40000 ALTER TABLE `hotels` DISABLE KEYS */;
INSERT INTO `hotels` VALUES (1,'The Sapphire Suits','Cart, Mackenzie Road, Hathipaon, Sher Garhi, Mussoorie (U.K)','Mussoorie','Uttarakhand','India',NULL,'+91-9312794200','reservations@sapphiresuits.com','',5,'14:00:00','11:00:00',24,50.00,1.00,'INR','',1,'2026-03-02 21:42:05','2026-03-04 15:52:24');
/*!40000 ALTER TABLE `hotels` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `ordercatalog`
--

DROP TABLE IF EXISTS `ordercatalog`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `ordercatalog` (
  `CatalogId` int NOT NULL AUTO_INCREMENT,
  `HotelId` int NOT NULL,
  `Category` enum('RoomService','Restaurant','Laundry','MiniBar','ServiceRequest','AddOn') COLLATE utf8mb4_unicode_ci NOT NULL,
  `ItemName` varchar(200) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Description` text COLLATE utf8mb4_unicode_ci,
  `UnitPrice` decimal(10,2) NOT NULL DEFAULT '0.00',
  `Unit` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT 'per item',
  `TaxPercent` decimal(5,2) DEFAULT '0.00',
  `IsAvailable` tinyint(1) DEFAULT '1',
  `ImageUrl` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `SortOrder` int DEFAULT '0',
  `CreatedAt` datetime DEFAULT CURRENT_TIMESTAMP,
  `UpdatedAt` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`CatalogId`),
  KEY `idx_category` (`Category`),
  KEY `idx_hotel_cat` (`HotelId`,`Category`),
  CONSTRAINT `ordercatalog_ibfk_1` FOREIGN KEY (`HotelId`) REFERENCES `hotels` (`HotelId`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=36 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ordercatalog`
--

LOCK TABLES `ordercatalog` WRITE;
/*!40000 ALTER TABLE `ordercatalog` DISABLE KEYS */;
INSERT INTO `ordercatalog` VALUES (1,1,'RoomService','Club Sandwich','Grilled chicken, bacon, lettuce, tomato on toasted bread',650.00,'per item',5.00,1,NULL,1,'2026-03-04 16:58:41','2026-03-04 16:58:41'),(2,1,'RoomService','Masala Omelette','3-egg omelette with onions, tomatoes, coriander',420.00,'per item',5.00,1,NULL,2,'2026-03-04 16:58:41','2026-03-04 16:58:41'),(3,1,'RoomService','Continental Breakfast','Croissant, fruit bowl, OJ, coffee or tea',850.00,'per item',5.00,1,NULL,3,'2026-03-04 16:58:41','2026-03-04 16:58:41'),(4,1,'RoomService','Chicken Tikka','Marinated chicken pieces, mint chutney',950.00,'per item',5.00,1,NULL,4,'2026-03-04 16:58:41','2026-03-04 16:58:41'),(5,1,'RoomService','Fresh Juice — Orange / Watermelon / Pineapple','250 ml freshly squeezed',280.00,'per glass',5.00,1,NULL,5,'2026-03-04 16:58:41','2026-03-04 16:58:41'),(6,1,'RoomService','Pot of Tea / Americano','Choice of tea variety or black coffee',220.00,'per serving',5.00,1,NULL,6,'2026-03-04 16:58:41','2026-03-04 16:58:41'),(7,1,'Restaurant','Buffet Breakfast','Full hot & cold buffet',1200.00,'per cover',5.00,1,NULL,1,'2026-03-04 17:01:35','2026-03-04 17:01:35'),(8,1,'Restaurant','Lunch Set Menu','3-course set menu with soft drink',1800.00,'per cover',5.00,1,NULL,2,'2026-03-04 17:01:35','2026-03-04 17:01:35'),(9,1,'Restaurant','Dinner A La Carte','Individual order from dinner menu',0.00,'variable',5.00,1,NULL,3,'2026-03-04 17:01:35','2026-03-04 17:01:35'),(10,1,'Restaurant','High Tea','Sandwiches, scones, pastries, pot of tea',800.00,'per person',5.00,1,NULL,4,'2026-03-04 17:01:35','2026-03-04 17:01:35'),(11,1,'Restaurant','Private Dining Setup','Candle-lit setup, flowers, dedicated waiter',2500.00,'per occasion',18.00,1,NULL,5,'2026-03-04 17:01:35','2026-03-04 17:01:35'),(12,1,'Laundry','Shirt / Blouse (Wash & Iron)','',120.00,'per piece',5.00,1,NULL,1,'2026-03-04 17:01:35','2026-03-04 17:01:35'),(13,1,'Laundry','Trousers / Skirt (Wash & Iron)','',150.00,'per piece',5.00,1,NULL,2,'2026-03-04 17:01:35','2026-03-04 17:01:35'),(14,1,'Laundry','Suit / Blazer (Dry Clean)','',400.00,'per piece',5.00,1,NULL,3,'2026-03-04 17:01:35','2026-03-04 17:01:35'),(15,1,'Laundry','Saree / Lehenga (Dry Clean)','',500.00,'per piece',5.00,1,NULL,4,'2026-03-04 17:01:35','2026-03-04 17:01:35'),(16,1,'Laundry','Express Service (4 hr)','Additional surcharge on regular price',200.00,'per order',5.00,1,NULL,5,'2026-03-04 17:01:35','2026-03-04 17:01:35'),(17,1,'MiniBar','Assorted Nuts (50 g)','Cashews, almonds, pistachios',320.00,'per pack',18.00,1,NULL,1,'2026-03-04 17:01:35','2026-03-04 17:01:35'),(18,1,'MiniBar','Chocolate Bar (Assorted)','Premium selection',180.00,'per piece',18.00,1,NULL,2,'2026-03-04 17:01:35','2026-03-04 17:01:35'),(19,1,'MiniBar','Soft Drink (330 ml)','Coke, Sprite, Limca',220.00,'per can',18.00,1,NULL,3,'2026-03-04 17:01:35','2026-03-04 17:01:35'),(20,1,'MiniBar','Mineral Water (1 L)','',120.00,'per bottle',18.00,1,NULL,4,'2026-03-04 17:01:35','2026-03-04 17:01:35'),(21,1,'MiniBar','Beer (330 ml)','Kingfisher Premium',450.00,'per can',28.00,1,NULL,5,'2026-03-04 17:01:35','2026-03-04 17:01:35'),(22,1,'MiniBar','Wine (Sula Red / White)','187 ml mini bottle',550.00,'per bottle',28.00,1,NULL,6,'2026-03-04 17:01:35','2026-03-04 17:01:35'),(23,1,'MiniBar','Whisky (30 ml miniature)','Johnnie Walker Black',420.00,'per bottle',28.00,1,NULL,7,'2026-03-04 17:01:35','2026-03-04 17:01:35'),(24,1,'ServiceRequest','Extra Pillow / Blanket','',0.00,'per item',0.00,1,NULL,1,'2026-03-04 17:01:35','2026-03-04 17:01:35'),(25,1,'ServiceRequest','Iron & Ironing Board','Delivered to room',0.00,'per item',0.00,1,NULL,2,'2026-03-04 17:01:35','2026-03-04 17:01:35'),(26,1,'ServiceRequest','Baby Cot','Per night charge',500.00,'per night',5.00,1,NULL,3,'2026-03-04 17:01:35','2026-03-04 17:01:35'),(27,1,'ServiceRequest','Airport Transfer (Sedan)','One way, up to 40 km',2500.00,'per trip',5.00,1,NULL,4,'2026-03-04 17:01:35','2026-03-04 17:01:35'),(28,1,'ServiceRequest','Airport Transfer (SUV)','One way, up to 40 km',3500.00,'per trip',5.00,1,NULL,5,'2026-03-04 17:01:35','2026-03-04 17:01:35'),(29,1,'ServiceRequest','Spa Appointment','Select treatment at spa desk',0.00,'variable',18.00,1,NULL,6,'2026-03-04 17:01:35','2026-03-04 17:01:35'),(30,1,'ServiceRequest','Parking (per day)','Covered valet parking',400.00,'per day',18.00,1,NULL,7,'2026-03-04 17:01:35','2026-03-04 17:01:35'),(31,1,'AddOn','Honeymoon Decoration','Rose petal turndown, candles, balloon arch',3500.00,'per occasion',18.00,1,NULL,1,'2026-03-04 17:01:35','2026-03-04 17:01:35'),(32,1,'AddOn','Birthday Cake (500 g)','Customized cake with message',1800.00,'per item',5.00,1,NULL,2,'2026-03-04 17:01:35','2026-03-04 17:01:35'),(33,1,'AddOn','Flower Bouquet','Seasonal fresh flowers',1200.00,'per bouquet',5.00,1,NULL,3,'2026-03-04 17:01:35','2026-03-04 17:01:35'),(34,1,'AddOn','Late Checkout (till 4 PM)','Subject to availability',2000.00,'per booking',18.00,1,NULL,4,'2026-03-04 17:01:35','2026-03-04 17:01:35'),(35,1,'AddOn','Early Check-In (from 8 AM)','Subject to availability',2000.00,'per booking',18.00,1,NULL,5,'2026-03-04 17:01:35','2026-03-04 17:01:35');
/*!40000 ALTER TABLE `ordercatalog` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `orderitems`
--

DROP TABLE IF EXISTS `orderitems`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `orderitems` (
  `OrderItemId` int NOT NULL AUTO_INCREMENT,
  `OrderId` int NOT NULL,
  `CatalogId` int DEFAULT NULL,
  `ItemName` varchar(200) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Description` text COLLATE utf8mb4_unicode_ci,
  `Quantity` decimal(10,3) NOT NULL DEFAULT '1.000',
  `UnitPrice` decimal(10,2) NOT NULL,
  `TaxPercent` decimal(5,2) DEFAULT '0.00',
  `TaxAmount` decimal(10,2) GENERATED ALWAYS AS (round((((`Quantity` * `UnitPrice`) * `TaxPercent`) / 100),2)) STORED,
  `LineTotal` decimal(10,2) GENERATED ALWAYS AS (round((`Quantity` * `UnitPrice`),2)) STORED,
  `LineTotalWithTax` decimal(10,2) GENERATED ALWAYS AS (round(((`Quantity` * `UnitPrice`) * (1 + (`TaxPercent` / 100))),2)) STORED,
  `Notes` text COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`OrderItemId`),
  KEY `CatalogId` (`CatalogId`),
  KEY `idx_order` (`OrderId`),
  CONSTRAINT `orderitems_ibfk_1` FOREIGN KEY (`OrderId`) REFERENCES `orders` (`OrderId`) ON DELETE CASCADE,
  CONSTRAINT `orderitems_ibfk_2` FOREIGN KEY (`CatalogId`) REFERENCES `ordercatalog` (`CatalogId`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `orderitems`
--

LOCK TABLES `orderitems` WRITE;
/*!40000 ALTER TABLE `orderitems` DISABLE KEYS */;
/*!40000 ALTER TABLE `orderitems` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `orders`
--

DROP TABLE IF EXISTS `orders`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `orders` (
  `OrderId` int NOT NULL AUTO_INCREMENT,
  `OrderNumber` varchar(30) COLLATE utf8mb4_unicode_ci NOT NULL,
  `HotelId` int NOT NULL,
  `BookingId` int NOT NULL,
  `RoomId` int NOT NULL,
  `CustomerId` int NOT NULL,
  `Category` enum('RoomService','Restaurant','Laundry','MiniBar','ServiceRequest','AddOn') COLLATE utf8mb4_unicode_ci NOT NULL,
  `OrderStatus` enum('Pending','Confirmed','InProgress','Delivered','Cancelled','Billed') COLLATE utf8mb4_unicode_ci DEFAULT 'Pending',
  `Priority` enum('Normal','High','Urgent') COLLATE utf8mb4_unicode_ci DEFAULT 'Normal',
  `SubTotal` decimal(10,2) NOT NULL DEFAULT '0.00',
  `TaxAmount` decimal(10,2) NOT NULL DEFAULT '0.00',
  `DiscountAmount` decimal(10,2) NOT NULL DEFAULT '0.00',
  `GrandTotal` decimal(10,2) NOT NULL DEFAULT '0.00',
  `BillEntryId` int DEFAULT NULL,
  `SpecialInstructions` text COLLATE utf8mb4_unicode_ci,
  `InternalNotes` text COLLATE utf8mb4_unicode_ci,
  `DeliveryTime` datetime DEFAULT NULL,
  `CompletedAt` datetime DEFAULT NULL,
  `CancelledAt` datetime DEFAULT NULL,
  `CancelledBy` int DEFAULT NULL,
  `CancellationReason` text COLLATE utf8mb4_unicode_ci,
  `CreatedBy` int NOT NULL,
  `CreatedAt` datetime DEFAULT CURRENT_TIMESTAMP,
  `UpdatedAt` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`OrderId`),
  UNIQUE KEY `OrderNumber` (`OrderNumber`),
  KEY `HotelId` (`HotelId`),
  KEY `CustomerId` (`CustomerId`),
  KEY `idx_booking` (`BookingId`),
  KEY `idx_room` (`RoomId`),
  KEY `idx_status` (`OrderStatus`),
  KEY `idx_category` (`Category`),
  KEY `idx_created` (`CreatedAt`),
  CONSTRAINT `orders_ibfk_1` FOREIGN KEY (`HotelId`) REFERENCES `hotels` (`HotelId`),
  CONSTRAINT `orders_ibfk_2` FOREIGN KEY (`BookingId`) REFERENCES `bookings` (`BookingId`),
  CONSTRAINT `orders_ibfk_3` FOREIGN KEY (`RoomId`) REFERENCES `rooms` (`RoomId`),
  CONSTRAINT `orders_ibfk_4` FOREIGN KEY (`CustomerId`) REFERENCES `customers` (`CustomerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `orders`
--

LOCK TABLES `orders` WRITE;
/*!40000 ALTER TABLE `orders` DISABLE KEYS */;
/*!40000 ALTER TABLE `orders` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `orderstatushistory`
--

DROP TABLE IF EXISTS `orderstatushistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `orderstatushistory` (
  `HistoryId` int NOT NULL AUTO_INCREMENT,
  `OrderId` int NOT NULL,
  `OldStatus` varchar(30) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `NewStatus` varchar(30) COLLATE utf8mb4_unicode_ci NOT NULL,
  `ChangedBy` int DEFAULT NULL,
  `Notes` text COLLATE utf8mb4_unicode_ci,
  `ChangedAt` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`HistoryId`),
  KEY `idx_order` (`OrderId`),
  CONSTRAINT `orderstatushistory_ibfk_1` FOREIGN KEY (`OrderId`) REFERENCES `orders` (`OrderId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `orderstatushistory`
--

LOCK TABLES `orderstatushistory` WRITE;
/*!40000 ALTER TABLE `orderstatushistory` DISABLE KEYS */;
/*!40000 ALTER TABLE `orderstatushistory` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `partnerremittances`
--

DROP TABLE IF EXISTS `partnerremittances`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `partnerremittances` (
  `RemittanceId` int NOT NULL AUTO_INCREMENT,
  `PartnerId` int NOT NULL,
  `PeriodFrom` date NOT NULL,
  `PeriodTo` date NOT NULL,
  `TotalBookings` int DEFAULT '0',
  `GrossAmount` decimal(12,2) DEFAULT '0.00',
  `CommissionAmount` decimal(12,2) DEFAULT '0.00',
  `NetAmount` decimal(12,2) DEFAULT '0.00',
  `ReceivedAmount` decimal(12,2) DEFAULT '0.00',
  `Status` enum('Expected','Received','Partial','Overdue','Disputed') COLLATE utf8mb4_unicode_ci DEFAULT 'Expected',
  `ExpectedDate` date DEFAULT NULL,
  `ReceivedDate` date DEFAULT NULL,
  `TransactionRef` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Notes` text COLLATE utf8mb4_unicode_ci,
  `CreatedAt` datetime DEFAULT CURRENT_TIMESTAMP,
  `UpdatedAt` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`RemittanceId`),
  KEY `PartnerId` (`PartnerId`),
  CONSTRAINT `partnerremittances_ibfk_1` FOREIGN KEY (`PartnerId`) REFERENCES `channelpartners` (`PartnerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `partnerremittances`
--

LOCK TABLES `partnerremittances` WRITE;
/*!40000 ALTER TABLE `partnerremittances` DISABLE KEYS */;
/*!40000 ALTER TABLE `partnerremittances` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `payments`
--

DROP TABLE IF EXISTS `payments`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `payments` (
  `PaymentId` int NOT NULL AUTO_INCREMENT,
  `BookingId` int NOT NULL,
  `PaymentDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Amount` decimal(10,2) NOT NULL,
  `PaymentType` enum('Advance','Full','Partial','Refund','CancellationFee') COLLATE utf8mb4_unicode_ci DEFAULT 'Full',
  `PaymentMethod` enum('Cash','CreditCard','DebitCard','BankTransfer','UPI','Online','PartnerRemittance','Cheque') COLLATE utf8mb4_unicode_ci NOT NULL,
  `TransactionRef` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `GatewayName` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `GatewayTxnId` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Status` enum('Pending','Completed','Failed','Refunded','Disputed') COLLATE utf8mb4_unicode_ci DEFAULT 'Completed',
  `Notes` text COLLATE utf8mb4_unicode_ci,
  `ProcessedBy` int DEFAULT NULL,
  `CreatedAt` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`PaymentId`),
  KEY `BookingId` (`BookingId`),
  CONSTRAINT `payments_ibfk_1` FOREIGN KEY (`BookingId`) REFERENCES `bookings` (`BookingId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `payments`
--

LOCK TABLES `payments` WRITE;
/*!40000 ALTER TABLE `payments` DISABLE KEYS */;
/*!40000 ALTER TABLE `payments` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `roomavailability`
--

DROP TABLE IF EXISTS `roomavailability`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `roomavailability` (
  `AvailId` int NOT NULL AUTO_INCREMENT,
  `RoomTypeId` int NOT NULL,
  `AvailDate` date NOT NULL,
  `TotalRooms` int NOT NULL DEFAULT '0',
  `BlockedRooms` int NOT NULL DEFAULT '0',
  `BookedRooms` int NOT NULL DEFAULT '0',
  PRIMARY KEY (`AvailId`),
  UNIQUE KEY `uq_avail` (`RoomTypeId`,`AvailDate`),
  CONSTRAINT `roomavailability_ibfk_1` FOREIGN KEY (`RoomTypeId`) REFERENCES `roomtypes` (`RoomTypeId`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=369 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `roomavailability`
--

LOCK TABLES `roomavailability` WRITE;
/*!40000 ALTER TABLE `roomavailability` DISABLE KEYS */;
INSERT INTO `roomavailability` VALUES (1,4,'2026-03-02',3,0,0),(2,3,'2026-03-02',2,0,0),(3,2,'2026-03-02',4,0,0),(4,1,'2026-03-02',5,0,0),(5,4,'2026-03-03',3,0,0),(6,3,'2026-03-03',2,0,0),(7,2,'2026-03-03',4,0,0),(8,1,'2026-03-03',5,0,0),(9,4,'2026-03-04',3,0,0),(10,3,'2026-03-04',2,0,0),(11,2,'2026-03-04',4,0,0),(12,1,'2026-03-04',5,0,0),(13,4,'2026-03-05',3,0,0),(14,3,'2026-03-05',2,0,0),(15,2,'2026-03-05',4,0,0),(16,1,'2026-03-05',5,0,0),(17,4,'2026-03-06',3,0,0),(18,3,'2026-03-06',2,0,0),(19,2,'2026-03-06',4,0,0),(20,1,'2026-03-06',5,0,0),(21,4,'2026-03-07',3,0,0),(22,3,'2026-03-07',2,0,0),(23,2,'2026-03-07',4,0,0),(24,1,'2026-03-07',5,0,0),(25,4,'2026-03-08',3,0,0),(26,3,'2026-03-08',2,0,0),(27,2,'2026-03-08',4,0,0),(28,1,'2026-03-08',5,0,0),(29,4,'2026-03-09',3,0,0),(30,3,'2026-03-09',2,0,0),(31,2,'2026-03-09',4,0,1),(32,1,'2026-03-09',5,0,0),(33,4,'2026-03-10',3,0,0),(34,3,'2026-03-10',2,0,0),(35,2,'2026-03-10',4,0,0),(36,1,'2026-03-10',5,0,1),(37,4,'2026-03-11',3,0,0),(38,3,'2026-03-11',2,0,0),(39,2,'2026-03-11',4,0,0),(40,1,'2026-03-11',5,0,2),(41,4,'2026-03-12',3,0,0),(42,3,'2026-03-12',2,0,0),(43,2,'2026-03-12',4,0,0),(44,1,'2026-03-12',5,0,0),(45,4,'2026-03-13',3,0,0),(46,3,'2026-03-13',2,0,0),(47,2,'2026-03-13',4,0,0),(48,1,'2026-03-13',5,0,0),(49,4,'2026-03-14',3,0,0),(50,3,'2026-03-14',2,0,0),(51,2,'2026-03-14',4,0,0),(52,1,'2026-03-14',5,0,0),(53,4,'2026-03-15',3,0,0),(54,3,'2026-03-15',2,0,0),(55,2,'2026-03-15',4,0,0),(56,1,'2026-03-15',5,0,0),(57,4,'2026-03-16',3,0,0),(58,3,'2026-03-16',2,0,0),(59,2,'2026-03-16',4,0,0),(60,1,'2026-03-16',5,0,0),(61,4,'2026-03-17',3,0,0),(62,3,'2026-03-17',2,0,0),(63,2,'2026-03-17',4,0,0),(64,1,'2026-03-17',5,0,0),(65,4,'2026-03-18',3,0,0),(66,3,'2026-03-18',2,0,0),(67,2,'2026-03-18',4,0,0),(68,1,'2026-03-18',5,0,0),(69,4,'2026-03-19',3,0,0),(70,3,'2026-03-19',2,0,0),(71,2,'2026-03-19',4,0,0),(72,1,'2026-03-19',5,0,0),(73,4,'2026-03-20',3,0,0),(74,3,'2026-03-20',2,0,0),(75,2,'2026-03-20',4,0,0),(76,1,'2026-03-20',5,0,0),(77,4,'2026-03-21',3,0,0),(78,3,'2026-03-21',2,0,0),(79,2,'2026-03-21',4,0,0),(80,1,'2026-03-21',5,0,0),(81,4,'2026-03-22',3,0,0),(82,3,'2026-03-22',2,0,0),(83,2,'2026-03-22',4,0,0),(84,1,'2026-03-22',5,0,0),(85,4,'2026-03-23',3,0,0),(86,3,'2026-03-23',2,0,0),(87,2,'2026-03-23',4,0,0),(88,1,'2026-03-23',5,0,0),(89,4,'2026-03-24',3,0,0),(90,3,'2026-03-24',2,0,0),(91,2,'2026-03-24',4,0,0),(92,1,'2026-03-24',5,0,0),(93,4,'2026-03-25',3,0,0),(94,3,'2026-03-25',2,0,0),(95,2,'2026-03-25',4,0,0),(96,1,'2026-03-25',5,0,0),(97,4,'2026-03-26',3,0,0),(98,3,'2026-03-26',2,0,0),(99,2,'2026-03-26',4,0,0),(100,1,'2026-03-26',5,0,0),(101,4,'2026-03-27',3,0,0),(102,3,'2026-03-27',2,0,0),(103,2,'2026-03-27',4,0,0),(104,1,'2026-03-27',5,0,0),(105,4,'2026-03-28',3,0,0),(106,3,'2026-03-28',2,0,0),(107,2,'2026-03-28',4,0,0),(108,1,'2026-03-28',5,0,0),(109,4,'2026-03-29',3,0,0),(110,3,'2026-03-29',2,0,0),(111,2,'2026-03-29',4,0,0),(112,1,'2026-03-29',5,0,0),(113,4,'2026-03-30',3,0,0),(114,3,'2026-03-30',2,0,0),(115,2,'2026-03-30',4,0,0),(116,1,'2026-03-30',5,0,0),(117,4,'2026-03-31',3,0,0),(118,3,'2026-03-31',2,0,0),(119,2,'2026-03-31',4,0,0),(120,1,'2026-03-31',5,0,0),(121,4,'2026-04-01',3,0,0),(122,3,'2026-04-01',2,0,0),(123,2,'2026-04-01',4,0,0),(124,1,'2026-04-01',5,0,0),(125,4,'2026-04-02',3,0,0),(126,3,'2026-04-02',2,0,0),(127,2,'2026-04-02',4,0,0),(128,1,'2026-04-02',5,0,0),(129,4,'2026-04-03',3,0,0),(130,3,'2026-04-03',2,0,0),(131,2,'2026-04-03',4,0,0),(132,1,'2026-04-03',5,0,0),(133,4,'2026-04-04',3,0,0),(134,3,'2026-04-04',2,0,0),(135,2,'2026-04-04',4,0,0),(136,1,'2026-04-04',5,0,0),(137,4,'2026-04-05',3,0,0),(138,3,'2026-04-05',2,0,0),(139,2,'2026-04-05',4,0,0),(140,1,'2026-04-05',5,0,0),(141,4,'2026-04-06',3,0,0),(142,3,'2026-04-06',2,0,0),(143,2,'2026-04-06',4,0,0),(144,1,'2026-04-06',5,0,0),(145,4,'2026-04-07',3,0,0),(146,3,'2026-04-07',2,0,0),(147,2,'2026-04-07',4,0,0),(148,1,'2026-04-07',5,0,0),(149,4,'2026-04-08',3,0,0),(150,3,'2026-04-08',2,0,0),(151,2,'2026-04-08',4,0,0),(152,1,'2026-04-08',5,0,0),(153,4,'2026-04-09',3,0,0),(154,3,'2026-04-09',2,0,0),(155,2,'2026-04-09',4,0,0),(156,1,'2026-04-09',5,0,0),(157,4,'2026-04-10',3,0,0),(158,3,'2026-04-10',2,0,0),(159,2,'2026-04-10',4,0,0),(160,1,'2026-04-10',5,0,0),(161,4,'2026-04-11',3,0,0),(162,3,'2026-04-11',2,0,0),(163,2,'2026-04-11',4,0,0),(164,1,'2026-04-11',5,0,0),(165,4,'2026-04-12',3,0,0),(166,3,'2026-04-12',2,0,0),(167,2,'2026-04-12',4,0,0),(168,1,'2026-04-12',5,0,0),(169,4,'2026-04-13',3,0,0),(170,3,'2026-04-13',2,0,0),(171,2,'2026-04-13',4,0,0),(172,1,'2026-04-13',5,0,0),(173,4,'2026-04-14',3,0,0),(174,3,'2026-04-14',2,0,0),(175,2,'2026-04-14',4,0,0),(176,1,'2026-04-14',5,0,0),(177,4,'2026-04-15',3,0,0),(178,3,'2026-04-15',2,0,0),(179,2,'2026-04-15',4,0,0),(180,1,'2026-04-15',5,0,0),(181,4,'2026-04-16',3,0,0),(182,3,'2026-04-16',2,0,0),(183,2,'2026-04-16',4,0,0),(184,1,'2026-04-16',5,0,0),(185,4,'2026-04-17',3,0,0),(186,3,'2026-04-17',2,0,0),(187,2,'2026-04-17',4,0,0),(188,1,'2026-04-17',5,0,0),(189,4,'2026-04-18',3,0,0),(190,3,'2026-04-18',2,0,0),(191,2,'2026-04-18',4,0,0),(192,1,'2026-04-18',5,0,0),(193,4,'2026-04-19',3,0,0),(194,3,'2026-04-19',2,0,0),(195,2,'2026-04-19',4,0,0),(196,1,'2026-04-19',5,0,0),(197,4,'2026-04-20',3,0,0),(198,3,'2026-04-20',2,0,0),(199,2,'2026-04-20',4,0,0),(200,1,'2026-04-20',5,0,0),(201,4,'2026-04-21',3,0,0),(202,3,'2026-04-21',2,0,0),(203,2,'2026-04-21',4,0,0),(204,1,'2026-04-21',5,0,0),(205,4,'2026-04-22',3,0,0),(206,3,'2026-04-22',2,0,0),(207,2,'2026-04-22',4,0,0),(208,1,'2026-04-22',5,0,0),(209,4,'2026-04-23',3,0,0),(210,3,'2026-04-23',2,0,0),(211,2,'2026-04-23',4,0,0),(212,1,'2026-04-23',5,0,0),(213,4,'2026-04-24',3,0,0),(214,3,'2026-04-24',2,0,0),(215,2,'2026-04-24',4,0,0),(216,1,'2026-04-24',5,0,0),(217,4,'2026-04-25',3,0,0),(218,3,'2026-04-25',2,0,0),(219,2,'2026-04-25',4,0,0),(220,1,'2026-04-25',5,0,0),(221,4,'2026-04-26',3,0,0),(222,3,'2026-04-26',2,0,0),(223,2,'2026-04-26',4,0,0),(224,1,'2026-04-26',5,0,0),(225,4,'2026-04-27',3,0,0),(226,3,'2026-04-27',2,0,0),(227,2,'2026-04-27',4,0,0),(228,1,'2026-04-27',5,0,0),(229,4,'2026-04-28',3,0,0),(230,3,'2026-04-28',2,0,0),(231,2,'2026-04-28',4,0,0),(232,1,'2026-04-28',5,0,0),(233,4,'2026-04-29',3,0,0),(234,3,'2026-04-29',2,0,0),(235,2,'2026-04-29',4,0,0),(236,1,'2026-04-29',5,0,0),(237,4,'2026-04-30',3,0,0),(238,3,'2026-04-30',2,0,0),(239,2,'2026-04-30',4,0,0),(240,1,'2026-04-30',5,0,0),(241,4,'2026-05-01',3,0,0),(242,3,'2026-05-01',2,0,0),(243,2,'2026-05-01',4,0,0),(244,1,'2026-05-01',5,0,0),(245,4,'2026-05-02',3,0,0),(246,3,'2026-05-02',2,0,0),(247,2,'2026-05-02',4,0,0),(248,1,'2026-05-02',5,0,0),(249,4,'2026-05-03',3,0,0),(250,3,'2026-05-03',2,0,0),(251,2,'2026-05-03',4,0,0),(252,1,'2026-05-03',5,0,0),(253,4,'2026-05-04',3,0,0),(254,3,'2026-05-04',2,0,0),(255,2,'2026-05-04',4,0,0),(256,1,'2026-05-04',5,0,0),(257,4,'2026-05-05',3,0,0),(258,3,'2026-05-05',2,0,0),(259,2,'2026-05-05',4,0,0),(260,1,'2026-05-05',5,0,0),(261,4,'2026-05-06',3,0,0),(262,3,'2026-05-06',2,0,0),(263,2,'2026-05-06',4,0,0),(264,1,'2026-05-06',5,0,0),(265,4,'2026-05-07',3,0,0),(266,3,'2026-05-07',2,0,0),(267,2,'2026-05-07',4,0,0),(268,1,'2026-05-07',5,0,0),(269,4,'2026-05-08',3,0,0),(270,3,'2026-05-08',2,0,0),(271,2,'2026-05-08',4,0,0),(272,1,'2026-05-08',5,0,0),(273,4,'2026-05-09',3,0,0),(274,3,'2026-05-09',2,0,0),(275,2,'2026-05-09',4,0,0),(276,1,'2026-05-09',5,0,0),(277,4,'2026-05-10',3,0,0),(278,3,'2026-05-10',2,0,0),(279,2,'2026-05-10',4,0,0),(280,1,'2026-05-10',5,0,0),(281,4,'2026-05-11',3,0,0),(282,3,'2026-05-11',2,0,0),(283,2,'2026-05-11',4,0,0),(284,1,'2026-05-11',5,0,0),(285,4,'2026-05-12',3,0,0),(286,3,'2026-05-12',2,0,0),(287,2,'2026-05-12',4,0,0),(288,1,'2026-05-12',5,0,0),(289,4,'2026-05-13',3,0,0),(290,3,'2026-05-13',2,0,0),(291,2,'2026-05-13',4,0,0),(292,1,'2026-05-13',5,0,0),(293,4,'2026-05-14',3,0,0),(294,3,'2026-05-14',2,0,0),(295,2,'2026-05-14',4,0,0),(296,1,'2026-05-14',5,0,0),(297,4,'2026-05-15',3,0,0),(298,3,'2026-05-15',2,0,0),(299,2,'2026-05-15',4,0,0),(300,1,'2026-05-15',5,0,0),(301,4,'2026-05-16',3,0,0),(302,3,'2026-05-16',2,0,0),(303,2,'2026-05-16',4,0,0),(304,1,'2026-05-16',5,0,0),(305,4,'2026-05-17',3,0,0),(306,3,'2026-05-17',2,0,0),(307,2,'2026-05-17',4,0,0),(308,1,'2026-05-17',5,0,0),(309,4,'2026-05-18',3,0,0),(310,3,'2026-05-18',2,0,0),(311,2,'2026-05-18',4,0,0),(312,1,'2026-05-18',5,0,0),(313,4,'2026-05-19',3,0,0),(314,3,'2026-05-19',2,0,0),(315,2,'2026-05-19',4,0,0),(316,1,'2026-05-19',5,0,0),(317,4,'2026-05-20',3,0,0),(318,3,'2026-05-20',2,0,0),(319,2,'2026-05-20',4,0,0),(320,1,'2026-05-20',5,0,0),(321,4,'2026-05-21',3,0,0),(322,3,'2026-05-21',2,0,0),(323,2,'2026-05-21',4,0,0),(324,1,'2026-05-21',5,0,0),(325,4,'2026-05-22',3,0,0),(326,3,'2026-05-22',2,0,0),(327,2,'2026-05-22',4,0,0),(328,1,'2026-05-22',5,0,0),(329,4,'2026-05-23',3,0,0),(330,3,'2026-05-23',2,0,0),(331,2,'2026-05-23',4,0,0),(332,1,'2026-05-23',5,0,0),(333,4,'2026-05-24',3,0,0),(334,3,'2026-05-24',2,0,0),(335,2,'2026-05-24',4,0,0),(336,1,'2026-05-24',5,0,0),(337,4,'2026-05-25',3,0,0),(338,3,'2026-05-25',2,0,0),(339,2,'2026-05-25',4,0,0),(340,1,'2026-05-25',5,0,0),(341,4,'2026-05-26',3,0,0),(342,3,'2026-05-26',2,0,0),(343,2,'2026-05-26',4,0,0),(344,1,'2026-05-26',5,0,0),(345,4,'2026-05-27',3,0,0),(346,3,'2026-05-27',2,0,0),(347,2,'2026-05-27',4,0,0),(348,1,'2026-05-27',5,0,0),(349,4,'2026-05-28',3,0,0),(350,3,'2026-05-28',2,0,0),(351,2,'2026-05-28',4,0,0),(352,1,'2026-05-28',5,0,0),(353,4,'2026-05-29',3,0,0),(354,3,'2026-05-29',2,0,0),(355,2,'2026-05-29',4,0,0),(356,1,'2026-05-29',5,0,0),(357,4,'2026-05-30',3,0,0),(358,3,'2026-05-30',2,0,0),(359,2,'2026-05-30',4,0,0),(360,1,'2026-05-30',5,0,0);
/*!40000 ALTER TABLE `roomavailability` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `roomrates`
--

DROP TABLE IF EXISTS `roomrates`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `roomrates` (
  `RateId` int NOT NULL AUTO_INCREMENT,
  `RoomTypeId` int NOT NULL,
  `RateDate` date NOT NULL,
  `BaseRate` decimal(10,2) NOT NULL,
  `SpecialRate` decimal(10,2) DEFAULT NULL,
  `IsAvailable` tinyint(1) DEFAULT '1',
  `MinNights` int DEFAULT '1',
  `Notes` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `CreatedAt` datetime DEFAULT CURRENT_TIMESTAMP,
  `UpdatedAt` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`RateId`),
  UNIQUE KEY `uq_rate_date` (`RoomTypeId`,`RateDate`),
  CONSTRAINT `roomrates_ibfk_1` FOREIGN KEY (`RoomTypeId`) REFERENCES `roomtypes` (`RoomTypeId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `roomrates`
--

LOCK TABLES `roomrates` WRITE;
/*!40000 ALTER TABLE `roomrates` DISABLE KEYS */;
/*!40000 ALTER TABLE `roomrates` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `rooms`
--

DROP TABLE IF EXISTS `rooms`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `rooms` (
  `RoomId` int NOT NULL AUTO_INCREMENT,
  `HotelId` int NOT NULL,
  `RoomTypeId` int NOT NULL,
  `RoomNumber` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Floor` int DEFAULT NULL,
  `Status` enum('Available','Occupied','Maintenance','Blocked') COLLATE utf8mb4_unicode_ci DEFAULT 'Available',
  `Notes` text COLLATE utf8mb4_unicode_ci,
  `IsActive` tinyint(1) DEFAULT '1',
  `CreatedAt` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`RoomId`),
  UNIQUE KEY `uq_room_number` (`HotelId`,`RoomNumber`),
  KEY `RoomTypeId` (`RoomTypeId`),
  CONSTRAINT `rooms_ibfk_1` FOREIGN KEY (`HotelId`) REFERENCES `hotels` (`HotelId`),
  CONSTRAINT `rooms_ibfk_2` FOREIGN KEY (`RoomTypeId`) REFERENCES `roomtypes` (`RoomTypeId`)
) ENGINE=InnoDB AUTO_INCREMENT=15 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `rooms`
--

LOCK TABLES `rooms` WRITE;
/*!40000 ALTER TABLE `rooms` DISABLE KEYS */;
INSERT INTO `rooms` VALUES (1,1,1,'101',1,'Available',NULL,1,'2026-03-02 21:42:17'),(2,1,1,'102',1,'Available',NULL,1,'2026-03-02 21:42:17'),(3,1,1,'103',1,'Available',NULL,1,'2026-03-02 21:42:17'),(4,1,1,'104',1,'Available',NULL,1,'2026-03-02 21:42:17'),(5,1,1,'105',1,'Available',NULL,1,'2026-03-02 21:42:17'),(6,1,2,'201',2,'Available',NULL,1,'2026-03-02 21:42:17'),(7,1,2,'202',2,'Available',NULL,1,'2026-03-02 21:42:17'),(8,1,2,'203',2,'Available',NULL,1,'2026-03-02 21:42:17'),(9,1,2,'204',2,'Available',NULL,1,'2026-03-02 21:42:17'),(10,1,3,'301',3,'Available',NULL,1,'2026-03-02 21:42:17'),(11,1,3,'302',3,'Available',NULL,1,'2026-03-02 21:42:17'),(12,1,4,'111',1,'Available',NULL,1,'2026-03-02 21:42:17'),(13,1,4,'112',1,'Available',NULL,1,'2026-03-02 21:42:17'),(14,1,4,'113',1,'Available',NULL,1,'2026-03-02 21:42:17');
/*!40000 ALTER TABLE `rooms` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `roomtypes`
--

DROP TABLE IF EXISTS `roomtypes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `roomtypes` (
  `RoomTypeId` int NOT NULL AUTO_INCREMENT,
  `HotelId` int NOT NULL,
  `TypeName` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Description` text COLLATE utf8mb4_unicode_ci,
  `MaxOccupancy` int DEFAULT '2',
  `BedType` varchar(80) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `SizeInSqFt` decimal(8,2) DEFAULT NULL,
  `ViewType` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Amenities` json DEFAULT NULL,
  `ImageUrls` json DEFAULT NULL,
  `SortOrder` int DEFAULT '0',
  `IsActive` tinyint(1) DEFAULT '1',
  `CreatedAt` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`RoomTypeId`),
  KEY `HotelId` (`HotelId`),
  CONSTRAINT `roomtypes_ibfk_1` FOREIGN KEY (`HotelId`) REFERENCES `hotels` (`HotelId`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `roomtypes`
--

LOCK TABLES `roomtypes` WRITE;
/*!40000 ALTER TABLE `roomtypes` DISABLE KEYS */;
INSERT INTO `roomtypes` VALUES (1,1,'Standard Room','Comfortable modern room with city views.',2,'Queen',320.00,'City View','[\"Free WiFi\", \"Air Conditioning\", \"LED TV\", \"Mini Fridge\", \"Safe\", \"Tea/Coffee Maker\", \"Rain Shower\"]',NULL,1,1,'2026-03-02 21:42:10'),(2,1,'Deluxe Room','Spacious room with direct ocean views.',2,'King',420.00,'Sea View','[\"Free WiFi\", \"Air Conditioning\", \"55 Smart TV\", \"Mini Bar\", \"Safe\", \"Espresso Machine\", \"Bathtub & Shower\", \"Pillow Menu\"]',NULL,2,1,'2026-03-02 21:42:10'),(3,1,'Suite','Luxurious suite with separate living area.',4,'King',750.00,'Panoramic Sea View','[\"Free WiFi\", \"Air Conditioning\", \"65 Smart TV\", \"Full Bar\", \"In-Room Dining\", \"Jacuzzi\", \"Living Area\", \"Butler Service\"]',NULL,3,1,'2026-03-02 21:42:10'),(4,1,'Twin Room','Elegant twin room for business travelers.',2,'Twin',340.00,'City View','[\"Free WiFi\", \"Air Conditioning\", \"LED TV\", \"Mini Fridge\", \"Safe\", \"Work Desk\", \"Tea/Coffee\"]',NULL,4,1,'2026-03-02 21:42:10');
/*!40000 ALTER TABLE `roomtypes` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `systemsettings`
--

DROP TABLE IF EXISTS `systemsettings`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `systemsettings` (
  `SettingKey` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `SettingValue` text COLLATE utf8mb4_unicode_ci,
  `Description` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `UpdatedAt` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`SettingKey`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `systemsettings`
--

LOCK TABLES `systemsettings` WRITE;
/*!40000 ALTER TABLE `systemsettings` DISABLE KEYS */;
INSERT INTO `systemsettings` VALUES ('DEFAULT_CURRENCY','INR','Default currency','2026-03-02 21:43:25'),('ENABLE_ONLINE_BOOKING','true','Enable public booking','2026-03-02 21:43:25'),('GST_NUMBER','27AAACG0569P1ZT','Hotel GST number','2026-03-02 21:43:25'),('HOTEL_NAME','Sapphire Suits','Hotel display name','2026-03-03 18:17:24'),('INVOICE_PREFIX','GAH','Invoice prefix','2026-03-02 21:43:25'),('MAX_BOOKING_DAYS','365','Max advance booking days','2026-03-02 21:43:25');
/*!40000 ALTER TABLE `systemsettings` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `users` (
  `UserId` int NOT NULL AUTO_INCREMENT,
  `HotelId` int DEFAULT NULL,
  `Username` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `PasswordHash` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `FullName` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Email` varchar(150) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Phone` varchar(30) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Role` enum('SuperAdmin','HotelAdmin','FrontDesk','Reservations','Finance','ReportViewer') COLLATE utf8mb4_unicode_ci DEFAULT 'FrontDesk',
  `IsActive` tinyint(1) DEFAULT '1',
  `LastLoginAt` datetime DEFAULT NULL,
  `LoginAttempts` int DEFAULT '0',
  `LockedUntil` datetime DEFAULT NULL,
  `MustChangePass` tinyint(1) DEFAULT '0',
  `CreatedAt` datetime DEFAULT CURRENT_TIMESTAMP,
  `UpdatedAt` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`UserId`),
  UNIQUE KEY `Username` (`Username`),
  UNIQUE KEY `Email` (`Email`),
  KEY `HotelId` (`HotelId`),
  CONSTRAINT `users_ibfk_1` FOREIGN KEY (`HotelId`) REFERENCES `hotels` (`HotelId`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

LOCK TABLES `users` WRITE;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
INSERT INTO `users` VALUES (1,1,'admin','Admin@2024','System Administrator','admin@grandazure.com',NULL,'SuperAdmin',1,'2026-03-18 16:16:27',0,NULL,0,'2026-03-02 21:43:25','2026-03-18 16:16:27'),(2,1,'frontdesk','Admin@2024','Front Desk Manager','desk@grandazure.com',NULL,'FrontDesk',1,'2026-03-09 22:10:34',0,NULL,0,'2026-03-02 21:43:25','2026-03-09 22:10:34'),(3,1,'manager','Admin@2024','Hotel Manager','manager@grandazure.com',NULL,'HotelAdmin',1,'2026-03-09 22:36:23',0,NULL,0,'2026-03-02 21:43:25','2026-03-09 22:36:23');
/*!40000 ALTER TABLE `users` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Temporary view structure for view `vw_bookingdetails`
--

DROP TABLE IF EXISTS `vw_bookingdetails`;
/*!50001 DROP VIEW IF EXISTS `vw_bookingdetails`*/;
SET @saved_cs_client     = @@character_set_client;
/*!50503 SET character_set_client = utf8mb4 */;
/*!50001 CREATE VIEW `vw_bookingdetails` AS SELECT 
 1 AS `BookingId`,
 1 AS `BookingReference`,
 1 AS `BookingStatus`,
 1 AS `BookingSource`,
 1 AS `CheckInDate`,
 1 AS `CheckOutDate`,
 1 AS `TotalNights`,
 1 AS `AdultsCount`,
 1 AS `ChildrenCount`,
 1 AS `RoomRate`,
 1 AS `SubTotal`,
 1 AS `TaxAmount`,
 1 AS `DiscountAmount`,
 1 AS `GrandTotal`,
 1 AS `CommissionAmount`,
 1 AS `NetToHotel`,
 1 AS `PaymentMode`,
 1 AS `AmountPaid`,
 1 AS `BalanceDue`,
 1 AS `SpecialRequests`,
 1 AS `CancellationReason`,
 1 AS `CancellationCharge`,
 1 AS `CancelledAt`,
 1 AS `CheckedInAt`,
 1 AS `CheckedOutAt`,
 1 AS `ConfirmedAt`,
 1 AS `BookingDate`,
 1 AS `HotelName`,
 1 AS `CurrencyCode`,
 1 AS `RoomTypeName`,
 1 AS `RoomNumber`,
 1 AS `GuestName`,
 1 AS `GuestEmail`,
 1 AS `GuestPhone`,
 1 AS `Nationality`,
 1 AS `IDType`,
 1 AS `IDNumber`,
 1 AS `VIPStatus`,
 1 AS `ChannelName`,
 1 AS `PartnerCode`,
 1 AS `PartnerPaymentMode`*/;
SET character_set_client = @saved_cs_client;

--
-- Temporary view structure for view `vw_bookingfolio`
--

DROP TABLE IF EXISTS `vw_bookingfolio`;
/*!50001 DROP VIEW IF EXISTS `vw_bookingfolio`*/;
SET @saved_cs_client     = @@character_set_client;
/*!50503 SET character_set_client = utf8mb4 */;
/*!50001 CREATE VIEW `vw_bookingfolio` AS SELECT 
 1 AS `BillEntryId`,
 1 AS `BookingId`,
 1 AS `EntryType`,
 1 AS `Description`,
 1 AS `ReferenceId`,
 1 AS `ReferenceType`,
 1 AS `Amount`,
 1 AS `TaxAmount`,
 1 AS `GrandAmount`,
 1 AS `PostedAt`,
 1 AS `IsVoided`,
 1 AS `BookingReference`,
 1 AS `GuestName`,
 1 AS `RoomNumber`,
 1 AS `PostedByName`*/;
SET character_set_client = @saved_cs_client;

--
-- Temporary view structure for view `vw_channelrevenuesummary`
--

DROP TABLE IF EXISTS `vw_channelrevenuesummary`;
/*!50001 DROP VIEW IF EXISTS `vw_channelrevenuesummary`*/;
SET @saved_cs_client     = @@character_set_client;
/*!50503 SET character_set_client = utf8mb4 */;
/*!50001 CREATE VIEW `vw_channelrevenuesummary` AS SELECT 
 1 AS `ChannelName`,
 1 AS `PaymentMode`,
 1 AS `TotalBookings`,
 1 AS `ConfirmedBookings`,
 1 AS `CancelledBookings`,
 1 AS `GrossRevenue`,
 1 AS `TotalCommission`,
 1 AS `NetRevenue`,
 1 AS `AvgValue`*/;
SET character_set_client = @saved_cs_client;

--
-- Temporary view structure for view `vw_orderdetails`
--

DROP TABLE IF EXISTS `vw_orderdetails`;
/*!50001 DROP VIEW IF EXISTS `vw_orderdetails`*/;
SET @saved_cs_client     = @@character_set_client;
/*!50503 SET character_set_client = utf8mb4 */;
/*!50001 CREATE VIEW `vw_orderdetails` AS SELECT 
 1 AS `OrderId`,
 1 AS `OrderNumber`,
 1 AS `OrderStatus`,
 1 AS `Priority`,
 1 AS `Category`,
 1 AS `SubTotal`,
 1 AS `TaxAmount`,
 1 AS `DiscountAmount`,
 1 AS `GrandTotal`,
 1 AS `SpecialInstructions`,
 1 AS `DeliveryTime`,
 1 AS `CompletedAt`,
 1 AS `CancelledAt`,
 1 AS `OrderDate`,
 1 AS `BookingReference`,
 1 AS `CheckInDate`,
 1 AS `CheckOutDate`,
 1 AS `RoomNumber`,
 1 AS `RoomTypeName`,
 1 AS `GuestName`,
 1 AS `GuestPhone`,
 1 AS `GuestEmail`,
 1 AS `VIPStatus`,
 1 AS `CreatedByName`,
 1 AS `BillEntryId`,
 1 AS `IsBilled`*/;
SET character_set_client = @saved_cs_client;

--
-- Dumping events for database 'hotelchannelmanager'
--

--
-- Dumping routines for database 'hotelchannelmanager'
--
/*!50003 DROP PROCEDURE IF EXISTS `sp_BillOrder` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_BillOrder`(
  IN  p_OrderId  INT,
  IN  p_PostedBy INT,
  OUT p_BillId   INT,
  OUT p_Msg      VARCHAR(300)
)
BEGIN

  DECLARE v_BookingId INT;
  DECLARE v_Category  VARCHAR(30);
  DECLARE v_Desc      VARCHAR(500);
  DECLARE v_Sub       DECIMAL(10,2);
  DECLARE v_Tax       DECIMAL(10,2);
  DECLARE v_Total     DECIMAL(10,2);
  DECLARE v_Status    VARCHAR(30);
  DECLARE v_ExistingBillId INT;
  DECLARE v_OrderNum  VARCHAR(30);
  DECLARE v_Error     INT DEFAULT 0;

  -- Error handler
  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
      SET v_Error = 1;
      ROLLBACK;
      SET p_BillId = 0;
      SET p_Msg = 'ERROR: Database exception occurred while billing order.';
  END;

  START TRANSACTION;

  main_block: BEGIN

    -- Fetch order details
    SELECT BookingId, Category, SubTotal, TaxAmount, GrandTotal,
           OrderStatus, OrderNumber, BillEntryId
    INTO   v_BookingId, v_Category, v_Sub, v_Tax, v_Total,
           v_Status, v_OrderNum, v_ExistingBillId
    FROM Orders
    WHERE OrderId = p_OrderId
    LIMIT 1;

    -- Order not found
    IF v_BookingId IS NULL THEN
        SET p_BillId = 0;
        SET p_Msg = 'ERROR: Order not found';
        ROLLBACK;
        LEAVE main_block;
    END IF;

    -- Already billed
    IF v_ExistingBillId IS NOT NULL THEN
        SET p_BillId = v_ExistingBillId;
        SET p_Msg = 'ERROR: Order already billed';
        ROLLBACK;
        LEAVE main_block;
    END IF;

    -- Cancelled order check
    IF v_Status = 'Cancelled' THEN
        SET p_BillId = 0;
        SET p_Msg = 'ERROR: Cannot bill a cancelled order';
        ROLLBACK;
        LEAVE main_block;
    END IF;

    -- Optional: Only allow billing if Delivered
    IF v_Status <> 'Delivered' THEN
        SET p_BillId = 0;
        SET p_Msg = 'ERROR: Only Delivered orders can be billed';
        ROLLBACK;
        LEAVE main_block;
    END IF;

    -- Prepare description
    SET v_Desc = CONCAT(v_Category, ' - Order ', v_OrderNum);

    -- Insert bill entry
    INSERT INTO BillEntries(
        BookingId, EntryType, Description,
        ReferenceId, ReferenceType,
        Amount, TaxAmount, GrandAmount,
        PostedBy, PostedDate
    )
    VALUES(
        v_BookingId, v_Category, v_Desc,
        p_OrderId, 'Order',
        v_Sub, v_Tax, v_Total,
        p_PostedBy, NOW()
    );

    SET p_BillId = LAST_INSERT_ID();

    -- Update order as billed
    UPDATE Orders
    SET BillEntryId = p_BillId,
        OrderStatus = 'Billed'
    WHERE OrderId = p_OrderId;

    -- Insert status history
    INSERT INTO OrderStatusHistory(
        OrderId, OldStatus, NewStatus,
        ChangedBy, Notes, ChangedDate
    )
    VALUES(
        p_OrderId, v_Status, 'Billed',
        p_PostedBy,
        CONCAT('Bill entry #', p_BillId, ' created'),
        NOW()
    );

    COMMIT;

    SET p_Msg = CONCAT(
        'SUCCESS: Bill entry #',
        p_BillId,
        ' posted for order ',
        v_OrderNum
    );

  END main_block;

END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_CancelBooking` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_CancelBooking`(
  IN p_BookingId INT, 
  IN p_Reason TEXT, 
  IN p_By INT,
  OUT p_Msg VARCHAR(300), 
  OUT p_Charge DECIMAL(10,2)
)
main_block: BEGIN

  DECLARE v_CheckIn DATE; 
  DECLARE v_CheckOut DATE; 
  DECLARE v_Status VARCHAR(30);
  DECLARE v_RtId INT; 
  DECLARE v_Grand DECIMAL(10,2); 
  DECLARE v_HotelId INT;
  DECLARE v_PolicyHrs INT; 
  DECLARE v_ChargePct DECIMAL(5,2);
  DECLARE v_HrsToCI DECIMAL(10,2); 
  DECLARE v_CurDate DATE; 
  DECLARE v_CustId INT;

  SELECT CheckInDate,CheckOutDate,BookingStatus,
         RoomTypeId,GrandTotal,HotelId,CustomerId
  INTO v_CheckIn,v_CheckOut,v_Status,
       v_RtId,v_Grand,v_HotelId,v_CustId
  FROM Bookings 
  WHERE BookingId=p_BookingId;

  IF v_Status IS NULL THEN 
     SET p_Msg='ERROR: Not found'; 
     SET p_Charge=0; 
     LEAVE main_block; 
  END IF;

  IF v_Status IN('Cancelled','CheckedOut','NoShow') THEN
     SET p_Msg=CONCAT('ERROR: Cannot cancel - ',v_Status); 
     SET p_Charge=0; 
     LEAVE main_block;
  END IF;

  SELECT CancellationPolicyHours,
         LateCancelChargePercent
  INTO v_PolicyHrs,v_ChargePct
  FROM Hotels 
  WHERE HotelId=v_HotelId;

  SET v_HrsToCI = TIMESTAMPDIFF(HOUR,NOW(),TIMESTAMP(v_CheckIn));

  IF v_HrsToCI < COALESCE(v_PolicyHrs,24) THEN
     SET p_Charge = ROUND(v_Grand*COALESCE(v_ChargePct,50)/100,2);
  ELSE 
     SET p_Charge = 0; 
  END IF;

  UPDATE Bookings 
  SET BookingStatus='Cancelled',
      CancellationReason=p_Reason,
      CancellationCharge=p_Charge,
      CancelledAt=NOW(),
      CancelledBy=p_By
  WHERE BookingId=p_BookingId;

  SET v_CurDate=v_CheckIn;

  WHILE v_CurDate<v_CheckOut DO
     UPDATE RoomAvailability 
     SET BookedRooms=GREATEST(BookedRooms-1,0)
     WHERE RoomTypeId=v_RtId 
     AND AvailDate=v_CurDate;

     SET v_CurDate=DATE_ADD(v_CurDate,INTERVAL 1 DAY);
  END WHILE;

  UPDATE Customers 
  SET TotalStays=GREATEST(TotalStays-1,0) 
  WHERE CustomerId=v_CustId;

  SET p_Msg='SUCCESS: Booking cancelled';

END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_CreateBooking` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_CreateBooking`(
   IN p_HotelId INT, IN p_RtId INT, IN p_CustId INT, IN p_PId INT,
   IN p_CheckIn DATE, IN p_CheckOut DATE, IN p_Adults INT, IN p_Children INT,
   IN p_PayMode VARCHAR(20), IN p_Source VARCHAR(30), IN p_SpecReqs TEXT,
   OUT p_BookingId INT, OUT p_BookingRef VARCHAR(30), OUT p_Msg VARCHAR(300)
)
main_block: BEGIN

   DECLARE v_Rate DECIMAL(10,2) DEFAULT 0;
   DECLARE v_Sub DECIMAL(10,2) DEFAULT 0;
   DECLARE v_Tax DECIMAL(10,2) DEFAULT 0;
   DECLARE v_TaxRate DECIMAL(5,2) DEFAULT 12;
   DECLARE v_Comm DECIMAL(10,2) DEFAULT 0;
   DECLARE v_CommRate DECIMAL(5,2) DEFAULT 0;
   DECLARE v_Avail INT DEFAULT 0;
   DECLARE v_CurDate DATE;
   DECLARE v_LastRate DECIMAL(10,2) DEFAULT 0;

   SET p_BookingId = 0;

   IF DATEDIFF(p_CheckOut,p_CheckIn) <= 0 THEN
     SET p_Msg='ERROR: Check-out must be after check-in';
     LEAVE main_block;
   END IF;

   SET v_CurDate = p_CheckIn;

   WHILE v_CurDate < p_CheckOut DO
     SELECT COALESCE(TotalRooms-BlockedRooms-BookedRooms,0)
     INTO v_Avail
     FROM RoomAvailability
     WHERE RoomTypeId=p_RtId AND AvailDate=v_CurDate;

     IF COALESCE(v_Avail,0) <= 0 THEN
       SET p_Msg=CONCAT('ERROR: No rooms available on ',DATE_FORMAT(v_CurDate,'%d %b %Y'));
       LEAVE main_block;
     END IF;

     SET v_CurDate = DATE_ADD(v_CurDate,INTERVAL 1 DAY);
   END WHILE;

   -- rest of your code continues exactly same

END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_CreateOrder` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_CreateOrder`(
   IN  p_HotelId      INT,
   IN  p_BookingId    INT,
   IN  p_RoomId       INT,
   IN  p_CustomerId   INT,
   IN  p_Category     VARCHAR(30),
   IN  p_Priority     VARCHAR(10),
   IN  p_SpecInstr    TEXT,
   IN  p_DeliveryTime DATETIME,
   IN  p_CreatedBy    INT,
   OUT p_OrderId      INT,
   OUT p_OrderNumber  VARCHAR(30),
   OUT p_Msg          VARCHAR(300)
)
BEGIN

   DECLARE v_Status VARCHAR(30);
   DECLARE v_Error  INT DEFAULT 0;

   -- Error handler
   DECLARE EXIT HANDLER FOR SQLEXCEPTION
   BEGIN
      SET v_Error = 1;
      ROLLBACK;
      SET p_OrderId = 0;
      SET p_Msg = 'ERROR: Database exception occurred while creating order.';
   END;

   START TRANSACTION;

   main_block: BEGIN

      -- Get booking status
      SELECT BookingStatus
      INTO v_Status
      FROM Bookings
      WHERE BookingId = p_BookingId
      LIMIT 1;

      -- Validate booking exists
      IF v_Status IS NULL THEN
         SET p_OrderId = 0;
         SET p_Msg = 'ERROR: Booking not found.';
         ROLLBACK;
         LEAVE main_block;
      END IF;

      -- Validate booking is active
      IF v_Status NOT IN ('Confirmed','CheckedIn') THEN
         SET p_OrderId = 0;
         SET p_Msg = CONCAT(
            'ERROR: Booking is ', v_Status,
            '. Orders allowed only for Confirmed/CheckedIn bookings.'
         );
         ROLLBACK;
         LEAVE main_block;
      END IF;

      -- Generate safer order number
      SET p_OrderNumber = CONCAT(
         'ORD',
         DATE_FORMAT(NOW(),'%Y%m%d%H%i%s'),
         LPAD(FLOOR(RAND()*9999),4,'0')
      );

      -- Insert Order
      INSERT INTO Orders(
         OrderNumber, HotelId, BookingId, RoomId, CustomerId,
         Category, OrderStatus, Priority,
         SpecialInstructions, DeliveryTime, CreatedBy,
         CreatedDate
      )
      VALUES(
         p_OrderNumber, p_HotelId, p_BookingId, p_RoomId, p_CustomerId,
         p_Category, 'Pending',
         COALESCE(p_Priority,'Normal'),
         p_SpecInstr, p_DeliveryTime, p_CreatedBy,
         NOW()
      );

      SET p_OrderId = LAST_INSERT_ID();

      -- Insert Status History
      INSERT INTO OrderStatusHistory(
         OrderId, OldStatus, NewStatus, ChangedBy, Notes, ChangedDate
      )
      VALUES(
         p_OrderId, NULL, 'Pending', p_CreatedBy,
         'Order created', NOW()
      );

      COMMIT;

      SET p_Msg = CONCAT('SUCCESS: Order ', p_OrderNumber, ' created successfully.');

   END main_block;

END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_GenerateCheckoutInvoice` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_GenerateCheckoutInvoice`(
  IN  p_BookingId INT,
  IN  p_UserId    INT,
  OUT p_InvoiceId INT,
  OUT p_Msg       VARCHAR(300)
)
BEGIN

  DECLARE v_HotelId    INT;
  DECLARE v_CustId     INT;
  DECLARE v_RoomCharge DECIMAL(10,2);
  DECLARE v_SvcCharge  DECIMAL(10,2);
  DECLARE v_TaxTotal   DECIMAL(10,2);
  DECLARE v_DiscTotal  DECIMAL(10,2);
  DECLARE v_Grand      DECIMAL(10,2);
  DECLARE v_Paid       DECIMAL(10,2);
  DECLARE v_InvNum     VARCHAR(30);
  DECLARE v_ExistingId INT;
  DECLARE v_Prefix     VARCHAR(10);
  DECLARE v_Error      INT DEFAULT 0;

  -- Error handler
  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
      SET v_Error = 1;
      ROLLBACK;
      SET p_InvoiceId = 0;
      SET p_Msg = 'ERROR: Database exception while generating invoice.';
  END;

  START TRANSACTION;

  main_block: BEGIN

    -- Check if invoice already exists
    SELECT InvoiceId 
    INTO v_ExistingId
    FROM CheckoutInvoices 
    WHERE BookingId = p_BookingId 
    LIMIT 1;

    IF v_ExistingId IS NOT NULL THEN
        SET p_InvoiceId = v_ExistingId;
        SET p_Msg = CONCAT('INFO: Invoice already exists #', v_ExistingId);
        ROLLBACK;
        LEAVE main_block;
    END IF;

    -- Validate booking
    SELECT HotelId, CustomerId, GrandTotal
    INTO v_HotelId, v_CustId, v_RoomCharge
    FROM Bookings 
    WHERE BookingId = p_BookingId
    LIMIT 1;

    IF v_HotelId IS NULL THEN
        SET p_InvoiceId = 0;
        SET p_Msg = 'ERROR: Booking not found';
        ROLLBACK;
        LEAVE main_block;
    END IF;

    -- Sum all active (non-voided) bill entries
    SELECT
      COALESCE(SUM(CASE 
          WHEN EntryType NOT IN('Discount','Payment','Refund') AND IsVoided=0 
          THEN Amount ELSE 0 END), 0),

      COALESCE(SUM(CASE 
          WHEN EntryType NOT IN('Discount','Payment','Refund') AND IsVoided=0 
          THEN TaxAmount ELSE 0 END), 0),

      COALESCE(SUM(CASE 
          WHEN EntryType='Discount' AND IsVoided=0 
          THEN ABS(Amount) ELSE 0 END), 0),

      COALESCE(SUM(CASE 
          WHEN EntryType='Payment' AND IsVoided=0 
          THEN ABS(GrandAmount) ELSE 0 END), 0)

    INTO v_SvcCharge, v_TaxTotal, v_DiscTotal, v_Paid
    FROM BillEntries 
    WHERE BookingId = p_BookingId;

    -- Calculate grand total
    SET v_Grand = v_RoomCharge + v_SvcCharge + v_TaxTotal - v_DiscTotal;

    -- Get invoice prefix
    SELECT COALESCE(SettingValue,'INV') 
    INTO v_Prefix 
    FROM SystemSettings 
    WHERE SettingKey='INVOICE_PREFIX'
    LIMIT 1;

    -- Generate invoice number
    SET v_InvNum = CONCAT(
        v_Prefix, '-',
        DATE_FORMAT(NOW(),'%Y%m%d'), '-',
        LPAD(p_BookingId,5,'0')
    );

    -- Insert invoice
    INSERT INTO CheckoutInvoices(
        InvoiceNumber, BookingId, HotelId, CustomerId,
        RoomCharges, ServiceCharges, TaxTotal,
        DiscountTotal, GrandTotal, AmountPaid,
        Status, IssuedAt, IssuedBy
    )
    VALUES(
        v_InvNum, p_BookingId, v_HotelId, v_CustId,
        v_RoomCharge, v_SvcCharge, v_TaxTotal,
        v_DiscTotal, v_Grand, v_Paid,
        'Issued', NOW(), p_UserId
    );

    SET p_InvoiceId = LAST_INSERT_ID();

    COMMIT;

    SET p_Msg = CONCAT('SUCCESS: Invoice ', v_InvNum, ' generated');

  END main_block;

END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_GetEffectiveRate` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_GetEffectiveRate`(IN p_RtId INT, IN p_Date DATE, IN p_PId INT, OUT p_Rate DECIMAL(10,2))
BEGIN
  DECLARE v_Rate DECIMAL(10,2) DEFAULT 0;
  DECLARE v_Markup DECIMAL(5,2) DEFAULT 0;
  SELECT COALESCE(SpecialRate,BaseRate) INTO v_Rate FROM RoomRates
  WHERE RoomTypeId=p_RtId AND RateDate=p_Date AND IsAvailable=1 LIMIT 1;
  IF v_Rate IS NULL OR v_Rate=0 THEN
    IF WEEKDAY(p_Date) IN (4,5) THEN
      SELECT WeekendRate INTO v_Rate FROM DefaultRoomRates
      WHERE RoomTypeId=p_RtId AND EffectiveFrom<=p_Date AND (EffectiveTo IS NULL OR EffectiveTo>=p_Date)
      ORDER BY EffectiveFrom DESC LIMIT 1;
    ELSE
      SELECT WeekdayRate INTO v_Rate FROM DefaultRoomRates
      WHERE RoomTypeId=p_RtId AND EffectiveFrom<=p_Date AND (EffectiveTo IS NULL OR EffectiveTo>=p_Date)
      ORDER BY EffectiveFrom DESC LIMIT 1;
    END IF;
  END IF;
  SET v_Rate=COALESCE(v_Rate,0);
  IF p_PId IS NOT NULL AND p_PId>0 THEN
    SELECT COALESCE(MarkupPercent,0) INTO v_Markup FROM ChannelRateMappings
    WHERE PartnerId=p_PId AND RoomTypeId=p_RtId AND IsActive=1 LIMIT 1;
    SET v_Rate=v_Rate*(1+v_Markup/100);
  END IF;
  SET p_Rate=ROUND(v_Rate,2);
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_RecalcOrder` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_RecalcOrder`(IN p_OrderId INT)
BEGIN
  DECLARE v_Sub   DECIMAL(10,2);
  DECLARE v_Tax   DECIMAL(10,2);
  DECLARE v_Total DECIMAL(10,2);

  SELECT
    COALESCE(SUM(LineTotal), 0),
    COALESCE(SUM(TaxAmount), 0),
    COALESCE(SUM(LineTotalWithTax), 0)
  INTO v_Sub, v_Tax, v_Total
  FROM OrderItems WHERE OrderId = p_OrderId;

  UPDATE Orders
  SET SubTotal   = v_Sub,
      TaxAmount  = v_Tax,
      GrandTotal = v_Total
  WHERE OrderId  = p_OrderId;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_UpdateOrderStatus` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_UpdateOrderStatus`(
  IN p_OrderId   INT,
  IN p_NewStatus VARCHAR(30),
  IN p_UserId    INT,
  IN p_Notes     TEXT,
  OUT p_Msg      VARCHAR(300)
)
BEGIN

  DECLARE v_Old       VARCHAR(30);
  DECLARE v_BillId    INT;
  DECLARE v_Error     INT DEFAULT 0;

  -- Error handler
  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
      SET v_Error = 1;
      ROLLBACK;
      SET p_Msg = 'ERROR: Database exception occurred while updating order status.';
  END;

  START TRANSACTION;

  main_block: BEGIN

    -- Fetch current order status
    SELECT OrderStatus, BillEntryId
    INTO v_Old, v_BillId
    FROM Orders
    WHERE OrderId = p_OrderId
    LIMIT 1;

    -- Order not found
    IF v_Old IS NULL THEN
        SET p_Msg = 'ERROR: Order not found';
        ROLLBACK;
        LEAVE main_block;
    END IF;

    -- Prevent change if already cancelled
    IF v_Old = 'Cancelled' THEN
        SET p_Msg = 'ERROR: Cannot change status of a cancelled order';
        ROLLBACK;
        LEAVE main_block;
    END IF;

    -- Prevent duplicate status update
    IF v_Old = p_NewStatus THEN
        SET p_Msg = CONCAT('INFO: Order already in status ', p_NewStatus);
        ROLLBACK;
        LEAVE main_block;
    END IF;

    -- Update logic
    IF p_NewStatus = 'Delivered' THEN

        UPDATE Orders
        SET OrderStatus = p_NewStatus,
            CompletedAt = NOW()
        WHERE OrderId = p_OrderId;

    ELSEIF p_NewStatus = 'Cancelled' THEN

        UPDATE Orders
        SET OrderStatus = p_NewStatus,
            CancelledAt = NOW(),
            CancelledBy = p_UserId,
            CancellationReason = p_Notes
        WHERE OrderId = p_OrderId;

    ELSE

        UPDATE Orders
        SET OrderStatus = p_NewStatus
        WHERE OrderId = p_OrderId;

    END IF;

    -- Insert status history
    INSERT INTO OrderStatusHistory(
        OrderId, OldStatus, NewStatus, ChangedBy, Notes, ChangedDate
    )
    VALUES(
        p_OrderId, v_Old, p_NewStatus, p_UserId, p_Notes, NOW()
    );

    COMMIT;

    SET p_Msg = CONCAT('SUCCESS: Status changed from ',
                       v_Old, ' to ', p_NewStatus);

  END main_block;

END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Final view structure for view `vw_bookingdetails`
--

/*!50001 DROP VIEW IF EXISTS `vw_bookingdetails`*/;
/*!50001 SET @saved_cs_client          = @@character_set_client */;
/*!50001 SET @saved_cs_results         = @@character_set_results */;
/*!50001 SET @saved_col_connection     = @@collation_connection */;
/*!50001 SET character_set_client      = utf8mb4 */;
/*!50001 SET character_set_results     = utf8mb4 */;
/*!50001 SET collation_connection      = utf8mb4_0900_ai_ci */;
/*!50001 CREATE ALGORITHM=UNDEFINED */
/*!50013 DEFINER=`root`@`localhost` SQL SECURITY DEFINER */
/*!50001 VIEW `vw_bookingdetails` AS select `b`.`BookingId` AS `BookingId`,`b`.`BookingReference` AS `BookingReference`,`b`.`BookingStatus` AS `BookingStatus`,`b`.`BookingSource` AS `BookingSource`,`b`.`CheckInDate` AS `CheckInDate`,`b`.`CheckOutDate` AS `CheckOutDate`,`b`.`TotalNights` AS `TotalNights`,`b`.`AdultsCount` AS `AdultsCount`,`b`.`ChildrenCount` AS `ChildrenCount`,`b`.`RoomRate` AS `RoomRate`,`b`.`SubTotal` AS `SubTotal`,`b`.`TaxAmount` AS `TaxAmount`,`b`.`DiscountAmount` AS `DiscountAmount`,`b`.`GrandTotal` AS `GrandTotal`,`b`.`CommissionAmount` AS `CommissionAmount`,`b`.`NetToHotel` AS `NetToHotel`,`b`.`PaymentMode` AS `PaymentMode`,`b`.`AmountPaid` AS `AmountPaid`,`b`.`BalanceDue` AS `BalanceDue`,`b`.`SpecialRequests` AS `SpecialRequests`,`b`.`CancellationReason` AS `CancellationReason`,`b`.`CancellationCharge` AS `CancellationCharge`,`b`.`CancelledAt` AS `CancelledAt`,`b`.`CheckedInAt` AS `CheckedInAt`,`b`.`CheckedOutAt` AS `CheckedOutAt`,`b`.`ConfirmedAt` AS `ConfirmedAt`,`b`.`CreatedAt` AS `BookingDate`,`h`.`HotelName` AS `HotelName`,`h`.`CurrencyCode` AS `CurrencyCode`,`rt`.`TypeName` AS `RoomTypeName`,`r`.`RoomNumber` AS `RoomNumber`,concat(`c`.`FirstName`,' ',`c`.`LastName`) AS `GuestName`,`c`.`Email` AS `GuestEmail`,`c`.`Phone` AS `GuestPhone`,`c`.`Nationality` AS `Nationality`,`c`.`IDType` AS `IDType`,`c`.`IDNumber` AS `IDNumber`,`c`.`VIPStatus` AS `VIPStatus`,coalesce(`cp`.`PartnerName`,'Direct') AS `ChannelName`,`cp`.`PartnerCode` AS `PartnerCode`,`cp`.`PaymentMode` AS `PartnerPaymentMode` from (((((`bookings` `b` join `hotels` `h` on((`h`.`HotelId` = `b`.`HotelId`))) join `roomtypes` `rt` on((`rt`.`RoomTypeId` = `b`.`RoomTypeId`))) left join `rooms` `r` on((`r`.`RoomId` = `b`.`RoomId`))) join `customers` `c` on((`c`.`CustomerId` = `b`.`CustomerId`))) left join `channelpartners` `cp` on((`cp`.`PartnerId` = `b`.`PartnerId`))) */;
/*!50001 SET character_set_client      = @saved_cs_client */;
/*!50001 SET character_set_results     = @saved_cs_results */;
/*!50001 SET collation_connection      = @saved_col_connection */;

--
-- Final view structure for view `vw_bookingfolio`
--

/*!50001 DROP VIEW IF EXISTS `vw_bookingfolio`*/;
/*!50001 SET @saved_cs_client          = @@character_set_client */;
/*!50001 SET @saved_cs_results         = @@character_set_results */;
/*!50001 SET @saved_col_connection     = @@collation_connection */;
/*!50001 SET character_set_client      = utf8mb4 */;
/*!50001 SET character_set_results     = utf8mb4 */;
/*!50001 SET collation_connection      = utf8mb4_0900_ai_ci */;
/*!50001 CREATE ALGORITHM=UNDEFINED */
/*!50013 DEFINER=`root`@`localhost` SQL SECURITY DEFINER */
/*!50001 VIEW `vw_bookingfolio` AS select `be`.`BillEntryId` AS `BillEntryId`,`be`.`BookingId` AS `BookingId`,`be`.`EntryType` AS `EntryType`,`be`.`Description` AS `Description`,`be`.`ReferenceId` AS `ReferenceId`,`be`.`ReferenceType` AS `ReferenceType`,`be`.`Amount` AS `Amount`,`be`.`TaxAmount` AS `TaxAmount`,`be`.`GrandAmount` AS `GrandAmount`,`be`.`PostedAt` AS `PostedAt`,`be`.`IsVoided` AS `IsVoided`,`b`.`BookingReference` AS `BookingReference`,concat(`c`.`FirstName`,' ',`c`.`LastName`) AS `GuestName`,`r`.`RoomNumber` AS `RoomNumber`,`u`.`FullName` AS `PostedByName` from ((((`billentries` `be` join `bookings` `b` on((`b`.`BookingId` = `be`.`BookingId`))) join `customers` `c` on((`c`.`CustomerId` = `b`.`CustomerId`))) left join `rooms` `r` on((`r`.`RoomId` = `b`.`RoomId`))) left join `users` `u` on((`u`.`UserId` = `be`.`PostedBy`))) */;
/*!50001 SET character_set_client      = @saved_cs_client */;
/*!50001 SET character_set_results     = @saved_cs_results */;
/*!50001 SET collation_connection      = @saved_col_connection */;

--
-- Final view structure for view `vw_channelrevenuesummary`
--

/*!50001 DROP VIEW IF EXISTS `vw_channelrevenuesummary`*/;
/*!50001 SET @saved_cs_client          = @@character_set_client */;
/*!50001 SET @saved_cs_results         = @@character_set_results */;
/*!50001 SET @saved_col_connection     = @@collation_connection */;
/*!50001 SET character_set_client      = utf8mb4 */;
/*!50001 SET character_set_results     = utf8mb4 */;
/*!50001 SET collation_connection      = utf8mb4_0900_ai_ci */;
/*!50001 CREATE ALGORITHM=UNDEFINED */
/*!50013 DEFINER=`root`@`localhost` SQL SECURITY DEFINER */
/*!50001 VIEW `vw_channelrevenuesummary` AS select coalesce(`cp`.`PartnerName`,'Direct / Walk-In') AS `ChannelName`,coalesce(`cp`.`PaymentMode`,'PayAtHotel') AS `PaymentMode`,count(`b`.`BookingId`) AS `TotalBookings`,sum((case when (`b`.`BookingStatus` <> 'Cancelled') then 1 else 0 end)) AS `ConfirmedBookings`,sum((case when (`b`.`BookingStatus` = 'Cancelled') then 1 else 0 end)) AS `CancelledBookings`,coalesce(sum((case when (`b`.`BookingStatus` <> 'Cancelled') then `b`.`GrandTotal` else 0 end)),0) AS `GrossRevenue`,coalesce(sum((case when (`b`.`BookingStatus` <> 'Cancelled') then `b`.`CommissionAmount` else 0 end)),0) AS `TotalCommission`,coalesce(sum((case when (`b`.`BookingStatus` <> 'Cancelled') then `b`.`NetToHotel` else 0 end)),0) AS `NetRevenue`,coalesce(avg((case when (`b`.`BookingStatus` <> 'Cancelled') then `b`.`GrandTotal` else NULL end)),0) AS `AvgValue` from (`bookings` `b` left join `channelpartners` `cp` on((`cp`.`PartnerId` = `b`.`PartnerId`))) group by `b`.`PartnerId`,`cp`.`PartnerName`,`cp`.`PaymentMode` */;
/*!50001 SET character_set_client      = @saved_cs_client */;
/*!50001 SET character_set_results     = @saved_cs_results */;
/*!50001 SET collation_connection      = @saved_col_connection */;

--
-- Final view structure for view `vw_orderdetails`
--

/*!50001 DROP VIEW IF EXISTS `vw_orderdetails`*/;
/*!50001 SET @saved_cs_client          = @@character_set_client */;
/*!50001 SET @saved_cs_results         = @@character_set_results */;
/*!50001 SET @saved_col_connection     = @@collation_connection */;
/*!50001 SET character_set_client      = utf8mb4 */;
/*!50001 SET character_set_results     = utf8mb4 */;
/*!50001 SET collation_connection      = utf8mb4_0900_ai_ci */;
/*!50001 CREATE ALGORITHM=UNDEFINED */
/*!50013 DEFINER=`root`@`localhost` SQL SECURITY DEFINER */
/*!50001 VIEW `vw_orderdetails` AS select `o`.`OrderId` AS `OrderId`,`o`.`OrderNumber` AS `OrderNumber`,`o`.`OrderStatus` AS `OrderStatus`,`o`.`Priority` AS `Priority`,`o`.`Category` AS `Category`,`o`.`SubTotal` AS `SubTotal`,`o`.`TaxAmount` AS `TaxAmount`,`o`.`DiscountAmount` AS `DiscountAmount`,`o`.`GrandTotal` AS `GrandTotal`,`o`.`SpecialInstructions` AS `SpecialInstructions`,`o`.`DeliveryTime` AS `DeliveryTime`,`o`.`CompletedAt` AS `CompletedAt`,`o`.`CancelledAt` AS `CancelledAt`,`o`.`CreatedAt` AS `OrderDate`,`b`.`BookingReference` AS `BookingReference`,`b`.`CheckInDate` AS `CheckInDate`,`b`.`CheckOutDate` AS `CheckOutDate`,`r`.`RoomNumber` AS `RoomNumber`,`rt`.`TypeName` AS `RoomTypeName`,concat(`c`.`FirstName`,' ',`c`.`LastName`) AS `GuestName`,`c`.`Phone` AS `GuestPhone`,`c`.`Email` AS `GuestEmail`,`c`.`VIPStatus` AS `VIPStatus`,`u`.`FullName` AS `CreatedByName`,`o`.`BillEntryId` AS `BillEntryId`,(`o`.`BillEntryId` is not null) AS `IsBilled` from (((((`orders` `o` join `bookings` `b` on((`b`.`BookingId` = `o`.`BookingId`))) join `rooms` `r` on((`r`.`RoomId` = `o`.`RoomId`))) join `roomtypes` `rt` on((`rt`.`RoomTypeId` = `r`.`RoomTypeId`))) join `customers` `c` on((`c`.`CustomerId` = `o`.`CustomerId`))) left join `users` `u` on((`u`.`UserId` = `o`.`CreatedBy`))) */;
/*!50001 SET character_set_client      = @saved_cs_client */;
/*!50001 SET character_set_results     = @saved_cs_results */;
/*!50001 SET collation_connection      = @saved_col_connection */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-03-18 16:22:09
