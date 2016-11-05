Rails.application.routes.draw do
  root 'student#index'

  get 'login' => 'sessions#new'
  post 'login' => 'sessions#create'

  get 'student' => 'students#index'

  resources :users
end
