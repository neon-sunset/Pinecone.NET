# TODO

## General

- [x] Finish gRPC transport implementation
- [ ] Finish REST transport implementation
  - [ ] Custom converters for MetadataMap, MetadataValue and MetadataValue[]
  - [ ] Json contracts and property attributes definition
  - [ ] Client (boilerplate) implementation
  - [ ] Solve the conflict between 'required' properties from C# 12 and STJ SrcGen targeting net7.0
~~- [ ] Switch from hand-written approach to code generation for REST API~~
- [x] Switch from Golang library .proto contract to the Rust one because the former has a dependency/import graph
    which hits an edge case in protoc-csharp plugin preventing it from being usable
- [ ] Standard CI/CD+publishing config with dotnet-releaser, initial nuget release
- [ ] Test coverage

## Impl

- [ ] Devise a good container implementatioon for dynamically-ish typed metadata objects used for filtering. Should I use 'OneOf' nuget package?
- [ ] Debug actual Pinecone API behavior when querying vectors, there seem to be multiple conflicting(?) fields in QueryResponse (although Rust impl. suggests that only .Matches is valid)
- [ ] Add an extension to create an index and then wait for it to be ready to use, maybe add guards before sending requests?
- [ ] Restructure the solution and add Pinecone.SemanticKernel project for long-term memory integration? Or is it better to directly contribute it to SK?
