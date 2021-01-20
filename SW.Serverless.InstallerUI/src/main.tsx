import React, {useState, useEffect} from "react";
import path from "path";
import {Connection} from "./model"
import install from "./installer-cli-port"
import fs from "fs";
import {
  Container,
  Row,
  Col,
  ListGroup,
  ListGroupItem,
  Form,
  FormGroup,
  Button,
  Spinner,
  Modal
} from "react-bootstrap"




const arraysEqual = (a1: Connection[], a2: Connection[]) => {
  if (a1.length !== a2.length) return false;

  const a1Sorted = a1.concat().sort();
  const a2Sorted = a2.concat().sort();


  for (let i = 0; i < a1.length; ++i) {
    const a1Con = a1Sorted[i];
    const a2Con = a2Sorted[i];
    if (a1Con.secretKey !== a2Con.secretKey ||
      a1Con.accessKey !== a2Con.accessKey ||
      a1Con.bucket !== a2Con.bucket ||
      a1Con.endpoint !== a2Con.endpoint)
      return false;
  }

  return true;
}


const Main = () => {
  process.noAsar = true;

  const optionsPath = path.join(__dirname, "../options.json");


  const isReadyToInstall = () => {
    if (!chosenConnection)
      return false;
    if (!adapterId)
      return false;
    if (!adapterPath)
      return false;

    return true;

  }

  const [connections, changeConnections]: [c: Connection[], cc: Function] = useState([]);

  const [adapterId, changeAdapterId] = useState("");
  const [adapterPath, changeAdapterPath] = useState("");
  const [isRunning, setIsRunning] = useState(false);
  const [result, setResult] = useState<undefined | string>(undefined);
  const [errorOccured, setErrorOccured] = useState(false);
  const [connectionBucket, changeConnectionBucket] = useState("");
  const [connectionEndpoint, changeConnectionEndpoint] = useState("");
  const [connectionAccessKey, changeConnectionAccessKey] = useState("");
  const [connectionSecretKey, changeConnectionSecretKey] = useState("");
  const [stateConnectionsIsTruth, changeStateConnectionsIsTruth] = useState(false);
  const [chosenConnection, changeChosenConnection]: [c: Connection, cc: Function] = useState(undefined);
  const writeConnections = () => {

    fs.writeFileSync(optionsPath, JSON.stringify({
      connections: connections
    }))
  }

  const installAdapter = () => {
    setIsRunning(true);
    install(adapterPath, adapterId, chosenConnection, (result, isError) => {
      setErrorOccured(isError);
      setIsRunning(false);
      setResult(result);
    });

  }



  useEffect(() => {

    if (fs.existsSync(optionsPath)) {
      const optionsFile = fs.readFileSync(optionsPath, {
        encoding: "utf8"
      });
      const fileConnections: Connection[] = JSON.parse(optionsFile).connections;

      if (!arraysEqual(fileConnections, connections)) {

        if (stateConnectionsIsTruth) writeConnections();
        else changeConnections(fileConnections);

        changeStateConnectionsIsTruth(!stateConnectionsIsTruth);
      }

    }

    else {
      fs.writeFile(optionsPath, JSON.stringify({
        connections: []
      }), (err) => {
        if (err) {
        }
      });
    }
  })

  const deleteConnection = (con: Connection) => {
    changeStateConnectionsIsTruth(true);
    changeConnections(connections.filter(v => v.id !== con.id))
    changeChosenConnection(null);
  }

  const saveConnection = () => {
    const connection = {
      id: Math.random().toString(),
      secretKey: connectionSecretKey,
      accessKey: connectionAccessKey,
      bucket: connectionBucket,
      endpoint: connectionEndpoint
    }

    if (chosenConnection) {
      const idx = connections.findIndex(x => x.id === chosenConnection.id);
      const clone = connections.concat();
      clone[idx] = connection;
      changeConnections(clone);
    }
    else {
      changeConnections([...connections, connection]); changeStateConnectionsIsTruth(true)
    }
    changeChosenConnection(null);
  }


  const connectionsJSX: JSX.Element[] = connections.map(c =>
    <ListGroupItem key={c.id} active={chosenConnection && c.id === chosenConnection.id} onClick={(e) => {e.preventDefault(); changeChosenConnection(c);}} action> {c.bucket} [{c.endpoint}] </ListGroupItem>
  );

  useEffect(() => {
    if (chosenConnection) {
      changeConnectionEndpoint(chosenConnection.endpoint)
      changeConnectionBucket(chosenConnection.bucket)
      changeConnectionAccessKey(chosenConnection.accessKey)
      changeConnectionSecretKey(chosenConnection.secretKey)
    }
    else {
      changeConnectionEndpoint("")
      changeConnectionBucket("")
      changeConnectionAccessKey("")
      changeConnectionSecretKey("")

    }
  }, [chosenConnection])


  console.log(result)

  return (
    <Row>
      <Col>
        <Container className="mx-auto">
          <Row>
            <h1 className="font-weight-bold mt-2">Serverless Installer</h1>
          </Row>
          <Row className="my-3">
            <Col>
              <Form className="h-75">
                <ListGroupItem className="text-center font-weight-bold bg-dark text-white">
                  Connections
                </ListGroupItem>
                <ListGroup className="rounded" style={{
                  maxHeight: "12em",
                  overflow: "scroll"
                }}>
                  {connectionsJSX}
                </ListGroup>
              </Form>
            </Col>
            <Col>
              <Form className="h-100">
                <ListGroupItem className="text-center font-weight-bold bg-dark text-white">
                  Connection details
                </ListGroupItem>

                <div className="d-flex h-75 flex-column justify-content-between" style={{
                  minHeight: "12em",
                  height: "12em",
                  maxHeight: "12em",
                  overflow: "scroll"
                }}>
                  <Form.Control onChange={(e) => changeConnectionEndpoint(e.target.value)} value={connectionEndpoint} type="text" placeholder="Service Endpoint" />
                  <Form.Control onChange={(e) => changeConnectionBucket(e.target.value)} value={connectionBucket} type="text" placeholder="Bucket Name" />
                  <Form.Control onChange={(e) => changeConnectionAccessKey(e.target.value)} value={connectionAccessKey} type="text" placeholder="Access Key" />
                  <Form.Control onChange={(e) => changeConnectionSecretKey(e.target.value)} value={connectionSecretKey} type="text" placeholder="Secret Key" />
                </div>
              </Form>
            </Col>
          </Row>
          <Row className="mb-5">
            <Col>
              <Button disabled={!chosenConnection} onClick={() => deleteConnection(chosenConnection)} className="my-2 btn-danger w-100">Delete Connection </Button>
            </Col>
            <Col>
              <Row>
                <Col>
                  <Button
                    onClick={saveConnection}
                    disabled={!(connectionSecretKey && connectionAccessKey && connectionBucket && connectionEndpoint)}
                    className="my-2 btn-primary w-100">
                    {chosenConnection ? "Edit" : "Save new"} Connection
                  </Button>
                </Col>
                <Col sm={3} className={`${chosenConnection ? "" : "d-none"}`}>
                  <Button
                    onClick={() => changeChosenConnection(undefined)}
                    className="my-2 btn-primary w-100">
                    New
                 </Button>
                </Col>
              </Row>
            </Col>

          </Row>
          <Row>
            <Col>
              <Form className="w-100">
                <FormGroup>
                  <Row>
                    <Col>
                      <Form.Control onChange={(e) => changeAdapterId(e.target.value)} value={adapterId} placeholder="Adapter Id" />
                      <Form.Text className="text-muted">
                        Example: infolink.mappers.mappername
                  </Form.Text>
                    </Col>
                  </Row>
                </FormGroup>
                <FormGroup>
                  <Row>
                    <Col sm={2}>
                      <Form.Label className="btn btn-primary">Choose Adapter
                      <Form.File onChange={(e: any) => {if (e.target.files[0]) changeAdapterPath(e.target.files[0].path)}} className="d-none" />
                      </Form.Label>
                    </Col>
                    <Col>
                      <Form.Control readOnly value={adapterPath? adapterPath : "Choose file from browsing."} />
                    </Col>
                  </Row>
                </FormGroup>
              </Form>

            </Col>
          </Row>
          <Row>
            <Button disabled={!isReadyToInstall()} onClick={() => installAdapter()} className="mx-auto p-3 mb-5 h-100 mh-100 w-75 rounded btn-primary">
              {isRunning ?
                <Spinner animation="border" />
                :
                "Install adapter"
              }
            </Button>
          </Row>

          <Modal show={result ? true : false}>
            <Modal.Header closeButton>
              <Modal.Title>Result</Modal.Title>
            </Modal.Header>

            <Modal.Body>
              <p style={{color: errorOccured ? "red" : "green"}} >{result}</p>
            </Modal.Body>

            <Modal.Footer>
              <Button onClick={() => {setResult(null); setErrorOccured(false);}} variant="secondary">Ok</Button>
            </Modal.Footer>
          </Modal>

        </Container>
      </Col>
    </Row>
  )

}

export default Main;
