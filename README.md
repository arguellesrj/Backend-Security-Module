# Security and Password Recovery Module

## Overview
A backend system designed to handle user management and secure credential recovery flows. This project demonstrates practical implementation of security protocols, database management, and automated email services.

## Tech Stack
* **Language:** C#
* **Database:** Microsoft SQL Server (T-SQL)
* **Security:** SHA-256 Encryption
* **Integration:** SMTP (Mailtrap) for automated email delivery

## Key Features
* Generation of 6-digit temporary access codes.
* Secure storage of credentials using SHA-256 hashing.
* Implementation of Stored Procedures for logical validation and expiration control of access codes.
* Prevention of code reuse attacks through strict state management.
