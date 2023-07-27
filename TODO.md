# TODO

## General

- [x] Finish gRPC transport implementation
  - [x] (REST too) Add SparseVector support for query responses which I completely forgot about
  - [x] Figure out how to solve 'metadata' vs 'setMetadata' on PineconeVector serialization for Update and Upsert respectively
- [x] Finish REST transport implementation
  - [x] Custom converters for MetadataMap, MetadataValue and MetadataValue[]
    - [x] Maybe only MetadataValue converter is needed? Let's try that first
  - [x] Json contracts and property attributes definition
  - [x] Client (boilerplate) implementation
  - [x] Solve the conflict between 'required' properties from C# 12 and STJ SrcGen targeting net7.0
~~- [ ] Switch from hand-written approach to code generation for REST API~~
- [x] Switch from Golang library .proto contract to the Rust one because the former has a dependency/import graph
    which hits an edge case in protoc-csharp plugin preventing it from being usable
- [x] Implement support for Pinecone collections
- [x] Add CI/CD pipeline (dotnet-releaser)
- [x] Do final cleanup
- [x] Write README
- [x] Publish nuget
- [ ] Test coverage

## Nice to have

- [ ] It appears the behavior of the gRPC stack has changed and inner `RepeatedField<T>` buffer is no longer created to the exact length of data received in the response. Analyze the implementation changes and consider options to reduce copying and allocations.
- [ ] Add an extension to create an index and then wait for it to be ready to use, maybe add guards before sending requests?
- [ ] Restructure the solution and add Pinecone.SemanticKernel project for long-term memory integration? Or is it better to directly contribute it to SK?
- [ ] Consider putting the handling of "MetadataValue as DU" into a dedicated place instead of switches in multiple places. First-class DUs in C# when?
