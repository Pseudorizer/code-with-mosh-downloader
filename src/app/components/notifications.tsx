import React, {useEffect, useRef, useState} from 'react';
import {ipcRenderer} from 'electron';
import styled from 'styled-components';
import {AppNotification} from 'Types/types';
import {Close} from '@styled-icons/evaicons-solid';
import {animated, useTransition} from 'react-spring';

export type NotificationsProps = {}

const Container = styled.div`
  position: fixed;
  bottom: 0;
  right: 0;
  z-index: 1;
  display: flex;
  flex-direction: column;
  align-items: flex-end;
`;

const NotificationContainer = styled.div<{ backgroundColour: string, foregroundColour: string }>`
  background-color: ${props => props.backgroundColour};
  color: ${props => props.foregroundColour};
  border-radius: 10px;
  display: flex;
`;

const NotificationTextContainer = styled.div`
  flex: 5 1;
  padding: 0.6rem;
  display: flex;
`;

const NotificationText = styled.p`
  flex: 1 0;
  font-size: 12pt;
  font-weight: 500;
`;

const NotificationDismiss = styled.div<{ hoverColour: string }>`
  display: flex;
  flex: 1 0;
  min-height: 100%;
  border-top-right-radius: 10px;
  border-bottom-right-radius: 10px;
  align-items: center;
  justify-content: center;
  max-width: 30px;
  transition: .2s ease-in-out background-color;
  border-left: 1px solid #242424;

  &:hover { // #418B5A
    background-color: ${props => props.hoverColour};
    cursor: pointer;
  }
`;

const CloseIcon = styled(Close)`
  flex: 1;
  width: 20px;
  height: 20px;
`;

const colours = {
  'success': {
	foreground: '#000',
	background: '#7FC395',
	hover: '#51AC6E',
  },
  'info': {
	foreground: '#000',
	background: '#8FBDBD',
	hover: '#589595',
  },
  'error': {
	foreground: '#000',
	background: '#FD9B9B',
	hover: '#f56d6d',
  }
};

export function Notifications(props: NotificationsProps) {
  const [refMap] = useState(() => new WeakMap<AppNotification, HTMLElement>());
  const [notifications, setNotifications] = useState<AppNotification[]>([]);
  const closeRef = useRef<HTMLDivElement>();

  const transitions = useTransition(notifications, {
	from: {opacity: 0, height: 0, margin: '0 1rem 0.7rem 0.7rem', width: '450px', transform: 'translate(0px, 0px)'},
	enter: item => async next => await next({opacity: 1, height: refMap.get(item).offsetHeight}),
	leave: item => async next => {
	  await next({transform: 'translate(475px, 0px)'});
	  await next({height: 0, margin: 'unset'});
	},
	config: {
	  tension: 450,
	  friction: 65
	}
  });

  useEffect(() => {
	ipcRenderer.on('to-alerts', (event, args: AppNotification) => {
	  setNotifications(prev => [...prev, args]);

	  setTimeout(() => {
		setNotifications(prev => {
		  return prev.filter(x => x !== args);
		});
	  }, 3000);
	});
  }, []);

  const onCloseClick = (item: AppNotification) => {
	setNotifications(prev => {
	  return prev.filter(x => x !== item);
	});
  };

  return (
	<Container>
	  {transitions((style, item) => (
		<animated.div style={style}>
		  <NotificationContainer foregroundColour={colours[item.type].foreground}
								 backgroundColour={colours[item.type].background}
								 ref={ref => ref && refMap.set(item, ref)}>
			<NotificationTextContainer>
			  <NotificationText>{item.message}</NotificationText>
			</NotificationTextContainer>
			<NotificationDismiss hoverColour={colours[item.type].hover} ref={closeRef} onClick={() => onCloseClick(item)}>
			  <CloseIcon/>
			</NotificationDismiss>
		  </NotificationContainer>
		</animated.div>
	  ))}
	</Container>
  );
}
