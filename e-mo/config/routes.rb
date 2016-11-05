Rails.application.routes.draw do
  root 'students#index'

  get 'login' => 'sessions#new'
  post 'login' => 'sessions#create'

  get 'student' => 'students#index'
  get 'teacher' => 'teachers#index'
  post 'teacher/socket' => 'teachers#expression_data'

  resources :users
end
