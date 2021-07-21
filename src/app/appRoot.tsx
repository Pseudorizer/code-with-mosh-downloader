import React, {useRef} from 'react';
import styled from 'styled-components';
import GlobalDefault from 'App/styles/globalDefault';
import {ipcRenderer} from 'electron';

const RootContainer = styled.div`
  width: 100vw;
  height: 100vh;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
`;

export default function App() {
  const inputRef = useRef<HTMLInputElement>();

  const onAdd = async () => {
    const a = await ipcRenderer.invoke('to-enqueue', {url: 'https://codewithmosh.com/courses'});
  };

  return (
    <RootContainer>
      <GlobalDefault/>
      <input ref={inputRef} style={{width: '200px', backgroundColor: 'grey', flex: '1'}}/>
      <button onClick={() => onAdd()} style={{width: '200px', flex: '1'}}>add</button>
    </RootContainer>
  );
}
