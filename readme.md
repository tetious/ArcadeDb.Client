# ArcadeDb.Client

This is a simple ArcadeDb client/driver for .net that wraps the HTTP API. It is inspired by Dapper,
and should (hopefully) feel familiar enough to get started quickly.

## Usage

### ArcadeServer

Wraps the server level operations. (create database/list databases, etc.)

### ArcadeDatabase

Does the interesting bits like queries and serialization.

### (Extras) SimpleRepository

A basic reference document-style repository implementation.

More documentation to follow once things are a bit more fleshed out. For now the integration tests will hopefully help!

A nuget package is also forthcoming. 
